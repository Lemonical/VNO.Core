using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VNO.Core.Models;
using VNO.Core.Protocol;

namespace VNO.Core.Networking;

/// <summary>
/// A TCP based message client with a background receive loop
/// </summary>
/// <remarks>
/// This replaces the legacy TClientSocket used by both the client and server to
/// reach the auth server. It frames messages with <see cref="MessageFramer"/> so
/// partial reads are handled correctly. All methods are safe to call from any
/// thread, sends are serialized with a lock free single writer queue guard
/// </remarks>
public sealed class TcpMessageClient : IMessageClient
{
    private readonly ILogger<TcpMessageClient> _logger;
    private readonly MessageFramer _framer = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private TcpClient? _tcp;
    private NetworkStream? _stream;
    private CancellationTokenSource? _receiveCts;
    private Task? _receiveLoop;
    private ConnectionState _state = ConnectionState.Disconnected;

    /// <summary>
    /// Creates the client with a logger
    /// </summary>
    public TcpMessageClient(ILogger<TcpMessageClient> logger) => _logger = logger;

    /// <inheritdoc />
    public ConnectionState State => _state;

    /// <inheritdoc />
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <inheritdoc />
    public event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        await DisconnectAsync().ConfigureAwait(false);
        SetState(ConnectionState.Connecting);

        try
        {
            _tcp = new TcpClient { NoDelay = true };
            await _tcp.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
            _stream = _tcp.GetStream();

            _receiveCts = new CancellationTokenSource();
            var receiveToken = _receiveCts.Token;
            _receiveLoop = Task.Run(() => ReceiveLoopAsync(receiveToken), receiveToken);
            SetState(ConnectionState.Connected);
            _logger.LogInformation("Connected to {Host}:{Port}", host, port);
        }
        catch (Exception ex)
        {
            SetState(ConnectionState.Faulted, ex.Message);
            _logger.LogError(ex, "Failed to connect to {Host}:{Port}", host, port);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SendAsync(NetworkMessage message, CancellationToken cancellationToken = default)
    {
        var stream = _stream;
        if (stream is null || _state != ConnectionState.Connected)
        {
            _logger.LogWarning("Dropping message {Message}, link is not connected", message);
            return;
        }

        var bytes = MessageCodec.EncodeBytes(message);
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            SetState(ConnectionState.Faulted, ex.Message);
            _logger.LogError(ex, "Send failed");
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        if (_receiveCts is null && _tcp is null)
        {
            return;
        }

        SetState(ConnectionState.Closing);

        try
        {
            _receiveCts?.Cancel();
            if (_receiveLoop is not null)
            {
                await _receiveLoop.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // expected during shutdown
        }
        finally
        {
            _stream?.Dispose();
            _tcp?.Dispose();
            _receiveCts?.Dispose();
            _stream = null;
            _tcp = null;
            _receiveCts = null;
            _receiveLoop = null;
            SetState(ConnectionState.Disconnected);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
        _sendLock.Dispose();
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            while (!cancellationToken.IsCancellationRequested && _stream is not null)
            {
                var read = await _stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    // a zero length read means the peer closed the link
                    SetState(ConnectionState.Disconnected, "Peer closed the connection");
                    return;
                }

                foreach (var message in _framer.Append(buffer.AsSpan(0, read)))
                {
                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(string.Empty, message));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected during shutdown
        }
        catch (Exception ex)
        {
            SetState(ConnectionState.Faulted, ex.Message);
            _logger.LogError(ex, "Receive loop failed");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private void SetState(ConnectionState state, string? reason = null)
    {
        if (_state == state)
        {
            return;
        }

        _state = state;
        StateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(state, reason));
    }
}
