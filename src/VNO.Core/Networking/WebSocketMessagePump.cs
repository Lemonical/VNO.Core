using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VNO.Core.Protocol;

namespace VNO.Core.Networking;

/// <summary>
/// Drives one WebSocket, the shared engine behind both the client link and a server session
/// </summary>
/// <remarks>
/// Wraps the base <see cref="WebSocket"/> so it serves a <see cref="ClientWebSocket"/> and a
/// server accepted socket alike. Enforces the migration plan invariants in one place: a single
/// writer so sends never race, a bounded outbound queue so a slow reader cannot grow memory,
/// one frame per message so there is no stream reassembly, an inbound size cap, binary frames
/// only, and a decode failure that closes just this socket. Not part of the public surface,
/// the transports own it
/// </remarks>
internal sealed class WebSocketMessagePump : IAsyncDisposable
{
    private readonly WebSocket _socket;
    private readonly WebSocketTransportOptions _options;
    private readonly int _maxFieldCount;
    private readonly int _maxFieldLength;
    private readonly BackpressurePolicy _policy;
    private readonly ILogger _logger;
    private readonly Channel<NetworkMessage> _outbound;
    private readonly CancellationTokenSource _cts = new();

    private Task? _sendLoop;
    private Task? _receiveLoop;
    private int _closedSignalled;
    private WebSocketCloseStatus _closeStatus = WebSocketCloseStatus.NormalClosure;
    private string _closeDescription = string.Empty;

    /// <summary>
    /// Creates a pump over an open socket, does not start it until <see cref="Start"/>
    /// </summary>
    public WebSocketMessagePump(
        WebSocket socket,
        WebSocketTransportOptions options,
        BackpressurePolicy policy,
        ILogger logger,
        int maxFieldCount = ProtocolConstants.MaxFieldCount,
        int maxFieldLength = ProtocolConstants.MaxFieldLength)
    {
        _socket = socket;
        _options = options;
        _policy = policy;
        _logger = logger;
        _maxFieldCount = maxFieldCount;
        _maxFieldLength = maxFieldLength;

        var capacity = Math.Max(1, options.OutboundQueueCapacity);
        _outbound = Channel.CreateBounded<NetworkMessage>(new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            // the public send path may be called from many threads, only the drain reads
            SingleWriter = false,
            // Wait mode is used for both policies, under it a synchronous TryWrite returns
            // false when full so the CloseOnOverflow path can detect it, while the Wait
            // policy path awaits room through WriteAsync instead
            FullMode = BoundedChannelFullMode.Wait,
        });
    }

    /// <summary>
    /// Raised for each decoded inbound message
    /// </summary>
    public event Action<NetworkMessage>? MessageReceived;

    /// <summary>
    /// Raised once when the pump stops for any reason, carries a short reason
    /// </summary>
    public event Action<string>? Closed;

    /// <summary>
    /// Starts the send and receive loops
    /// </summary>
    public void Start()
    {
        _sendLoop = Task.Run(SendLoopAsync);
        _receiveLoop = Task.Run(ReceiveLoopAsync);
    }

    /// <summary>
    /// Queues a message for sending, honoring the backpressure policy
    /// </summary>
    /// <remarks>
    /// Under <see cref="BackpressurePolicy.Wait"/> this awaits room in the queue. Under
    /// <see cref="BackpressurePolicy.CloseOnOverflow"/> a full queue closes the session and
    /// the message is dropped, so one slow peer cannot exhaust memory
    /// </remarks>
    public async ValueTask SendAsync(NetworkMessage message, CancellationToken cancellationToken)
    {
        if (_closedSignalled != 0)
        {
            return;
        }

        if (_policy == BackpressurePolicy.Wait)
        {
            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
                await _outbound.Writer.WriteAsync(message, linked.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // closing or the caller cancelled, drop the message quietly
            }
            catch (ChannelClosedException)
            {
                // pump already stopped
            }

            return;
        }

        if (!_outbound.Writer.TryWrite(message))
        {
            _logger.LogWarning("Outbound queue overflow, closing session");
            Signal(WebSocketCloseStatus.InternalServerError, "outbound queue overflow");
        }
    }

    /// <summary>
    /// Requests a graceful close, drains queued sends within the deadline, then tears down
    /// </summary>
    public async Task CloseAsync(WebSocketCloseStatus status, string description, TimeSpan drainDeadline)
    {
        _closeStatus = status;
        _closeDescription = description;

        // stop accepting new sends and let the drain flush what is queued
        _outbound.Writer.TryComplete();

        if (_sendLoop is not null)
        {
            var completed = await Task.WhenAny(_sendLoop, Task.Delay(drainDeadline)).ConfigureAwait(false);
            if (completed != _sendLoop)
            {
                _logger.LogWarning("Drain deadline hit, forcing session close");
            }
        }

        Signal(status, description);
        await WaitForLoopsAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Signal(_closeStatus, _closeDescription);
        await WaitForLoopsAsync().ConfigureAwait(false);
        _cts.Dispose();
        _socket.Dispose();
    }

    private async Task SendLoopAsync()
    {
        try
        {
            while (await _outbound.Reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false))
            {
                while (_outbound.Reader.TryRead(out var message))
                {
                    var bytes = MessageCodec.EncodeFrameBytes(message);
                    await _socket.SendAsync(bytes, WebSocketMessageType.Binary, endOfMessage: true, _cts.Token)
                        .ConfigureAwait(false);
                }
            }

            // the queue was completed by a graceful close, tell the peer we are going away
            if (_socket.State == WebSocketState.Open)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _socket.CloseOutputAsync(_closeStatus, _closeDescription, timeout.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // cancelled by a hard close
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Send loop ended");
            Signal(WebSocketCloseStatus.InternalServerError, "send failure");
        }
    }

    private async Task ReceiveLoopAsync()
    {
        var buffer = ArrayPool<byte>.Shared.Rent(8192);
        var accumulator = new ArrayBufferWriter<byte>();
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (WebSocketException ex)
                {
                    _logger.LogDebug(ex, "Receive ended abruptly");
                    Signal(WebSocketCloseStatus.EndpointUnavailable, "receive failure");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Signal(WebSocketCloseStatus.NormalClosure, "peer closed");
                    break;
                }

                if (result.MessageType != WebSocketMessageType.Binary)
                {
                    // the vno.v2 subprotocol is binary only, a text frame is a protocol error
                    Signal(WebSocketCloseStatus.InvalidMessageType, "non binary frame");
                    break;
                }

                accumulator.Write(buffer.AsSpan(0, result.Count));
                if (accumulator.WrittenCount > _options.MaxInboundBytes)
                {
                    Signal(WebSocketCloseStatus.MessageTooBig, "frame exceeded size cap");
                    break;
                }

                if (!result.EndOfMessage)
                {
                    continue;
                }

                NetworkMessage message;
                try
                {
                    message = MessageCodec.DecodeFrame(accumulator.WrittenSpan, _maxFieldCount, _maxFieldLength);
                }
                catch (ProtocolFormatException ex)
                {
                    _logger.LogWarning("Rejecting malformed frame: {Reason}", ex.Message);
                    Signal(WebSocketCloseStatus.InvalidPayloadData, "malformed frame");
                    break;
                }

                accumulator.Clear();
                MessageReceived?.Invoke(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Receive loop ended");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            Signal(_closeStatus, _closeDescription);
        }
    }

    private void Signal(WebSocketCloseStatus status, string description)
    {
        if (Interlocked.Exchange(ref _closedSignalled, 1) != 0)
        {
            return;
        }

        _closeStatus = status;
        _closeDescription = description;
        _outbound.Writer.TryComplete();
        _cts.Cancel();
        Closed?.Invoke(description);
    }

    private async Task WaitForLoopsAsync()
    {
        try
        {
            if (_sendLoop is not null)
            {
                await _sendLoop.ConfigureAwait(false);
            }

            if (_receiveLoop is not null)
            {
                await _receiveLoop.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // expected during teardown
        }
    }
}
