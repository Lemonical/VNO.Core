using System;
using System.Buffers;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using VNO.Core.Protocol;

namespace VNO.Core.Networking;

/// <summary>
/// One accepted peer on the server, owns its socket and receive loop
/// </summary>
/// <remarks>
/// Internal to the networking layer, the server creates and tracks these. Each
/// session has its own framer so partial reads from one peer do not affect
/// another
/// </remarks>
internal sealed class TcpSession : IAsyncDisposable
{
    private readonly TcpClient _tcp;
    private readonly NetworkStream _stream;
    private readonly MessageFramer _framer;
    private readonly CancellationTokenSource _cts = new();
    private readonly Channel<NetworkMessage> _outbound = Channel.CreateBounded<NetworkMessage>(
        new BoundedChannelOptions(256)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait,
        });

    private Task? _receiveLoop;
    private Task? _sendLoop;
    private int _disposed;

    /// <summary>
    /// Creates a session around an accepted socket
    /// </summary>
    public TcpSession(string id, TcpClient tcp, int maximumMessageBytes)
    {
        Id = id;
        _tcp = tcp;
        _tcp.NoDelay = true;
        _stream = tcp.GetStream();
        _framer = new MessageFramer(maximumMessageBytes);
        RemoteAddress = (tcp.Client.RemoteEndPoint as System.Net.IPEndPoint)?.Address.ToString()
                        ?? string.Empty;
    }

    /// <summary>
    /// Stable id of this session
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Remote address of the peer
    /// </summary>
    public string RemoteAddress { get; }

    /// <summary>
    /// Raised when this session receives a complete message
    /// </summary>
    public event Action<TcpSession, NetworkMessage>? MessageReceived;

    /// <summary>
    /// Raised when this session ends for any reason
    /// </summary>
    public event Action<TcpSession>? Closed;

    /// <summary>
    /// Starts the background receive loop
    /// </summary>
    public void Start()
    {
        var token = _cts.Token;
        _receiveLoop = Task.Run(() => ReceiveLoopAsync(token), token);
        _sendLoop = Task.Run(() => SendLoopAsync(token), token);
    }

    /// <summary>
    /// Sends a message to this peer
    /// </summary>
    public Task SendAsync(NetworkMessage message, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }
        if (_outbound.Writer.TryWrite(message))
        {
            return Task.CompletedTask;
        }

        // A full bounded queue means this peer is not consuming quickly enough.
        // Close only this session so broadcasts to healthy peers keep moving.
        _cts.Cancel();
        _tcp.Close();
        return Task.FromException(new IOException("TCP outbound queue overflow"));
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        try
        {
            _outbound.Writer.TryComplete();
            await _cts.CancelAsync().ConfigureAwait(false);
            if (_receiveLoop is not null)
            {
                await _receiveLoop.ConfigureAwait(false);
            }
            if (_sendLoop is not null)
            {
                await _sendLoop.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // expected during shutdown
        }
        finally
        {
            _stream.Dispose();
            _tcp.Dispose();
            _cts.Dispose();
        }
    }

    private async Task SendLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in _outbound.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                var bytes = MessageCodec.EncodeBytes(message);
                await _stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // expected during shutdown
        }
        catch (Exception)
        {
            _cts.Cancel();
            _tcp.Close();
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var read = await _stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                foreach (var message in _framer.Append(buffer.AsSpan(0, read)))
                {
                    MessageReceived?.Invoke(this, message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected during shutdown
        }
        catch (Exception)
        {
            // any socket error ends the session, the server is told through Closed
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            Closed?.Invoke(this);
        }
    }
}
