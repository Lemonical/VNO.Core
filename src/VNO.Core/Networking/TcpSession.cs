using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly MessageFramer _framer = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly CancellationTokenSource _cts = new();

    private Task? _receiveLoop;

    /// <summary>
    /// Creates a session around an accepted socket
    /// </summary>
    public TcpSession(string id, TcpClient tcp)
    {
        Id = id;
        _tcp = tcp;
        _tcp.NoDelay = true;
        _stream = tcp.GetStream();
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
    }

    /// <summary>
    /// Sends a message to this peer
    /// </summary>
    public async Task SendAsync(NetworkMessage message, CancellationToken cancellationToken)
    {
        var bytes = MessageCodec.EncodeBytes(message);
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await _cts.CancelAsync().ConfigureAwait(false);
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
            _stream.Dispose();
            _tcp.Dispose();
            _cts.Dispose();
            _sendLock.Dispose();
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
