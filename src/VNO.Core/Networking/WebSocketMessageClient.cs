using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VNO.Core.Models;
using VNO.Core.Protocol;

namespace VNO.Core.Networking;

/// <summary>
/// A WebSocket message client with a single writer, keep alive, and reconnect with backoff
/// </summary>
/// <remarks>
/// Satisfies the same <see cref="IMessageClient"/> contract as <see cref="TcpMessageClient"/>,
/// so the client and game server links swap to it with only DI and endpoint changes. One frame
/// is one message, so there is no stream reassembly. The link is long lived, so when a healthy
/// connection drops the client reconnects with exponential backoff and jitter and raises
/// <see cref="StateChanged"/> to Connected again, at which point the app re runs its own login or
/// re announce. Session re establishment stays an application concern
/// </remarks>
public sealed class WebSocketMessageClient : IMessageClient
{
    private readonly ILogger<WebSocketMessageClient> _logger;
    private readonly WebSocketTransportOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private ClientWebSocket? _socket;
    private WebSocketMessagePump? _pump;
    private ConnectionState _state = ConnectionState.Disconnected;
    private string _host = string.Empty;
    private int _port;
    private volatile bool _shouldRun;
    private int _reconnecting;

    /// <summary>
    /// Creates the client with a logger and optional transport options
    /// </summary>
    public WebSocketMessageClient(ILogger<WebSocketMessageClient> logger, WebSocketTransportOptions? options = null)
    {
        _logger = logger;
        _options = options ?? new WebSocketTransportOptions();
    }

    /// <inheritdoc />
    public ConnectionState State => _state;

    /// <inheritdoc />
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <inheritdoc />
    public event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        _host = host;
        _port = port;
        _shouldRun = true;
        await ConnectOnceAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendAsync(NetworkMessage message, CancellationToken cancellationToken = default)
    {
        var pump = _pump;
        if (pump is null || _state != ConnectionState.Connected)
        {
            _logger.LogWarning("Dropping message {Message}, link is not connected", message);
            return;
        }

        await pump.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        _shouldRun = false;
        SetState(ConnectionState.Closing);
        await TearDownAsync().ConfigureAwait(false);
        SetState(ConnectionState.Disconnected);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
        _gate.Dispose();
    }

    private async Task ConnectOnceAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await TearDownPumpAsync().ConfigureAwait(false);
            SetState(ConnectionState.Connecting);

            var socket = new ClientWebSocket();
            socket.Options.AddSubProtocol(_options.Subprotocol);
            socket.Options.KeepAliveInterval = _options.KeepAliveInterval;

            var uri = BuildUri();
            try
            {
                await socket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                socket.Dispose();
                SetState(ConnectionState.Faulted, ex.Message);
                _logger.LogError(ex, "Failed to connect to {Uri}", uri);
                throw;
            }

            _socket = socket;
            _pump = new WebSocketMessagePump(socket, _options, BackpressurePolicy.Wait, _logger);
            _pump.MessageReceived += OnPumpMessage;
            _pump.Closed += OnPumpClosed;
            _pump.Start();

            SetState(ConnectionState.Connected);
            _logger.LogInformation("Connected to {Uri}", uri);
        }
        finally
        {
            _gate.Release();
        }
    }

    private Uri BuildUri()
    {
        var scheme = _options.UseTls ? "wss" : "ws";
        var defaultPort = _options.UseTls ? 443 : 80;
        var authority = _port == defaultPort ? _host : $"{_host}:{_port}";
        var path = _options.Path.StartsWith('/') ? _options.Path : "/" + _options.Path;
        return new Uri($"{scheme}://{authority}{path}");
    }

    private void OnPumpMessage(NetworkMessage message) =>
        MessageReceived?.Invoke(this, new MessageReceivedEventArgs(string.Empty, message));

    private void OnPumpClosed(string reason)
    {
        SetState(ConnectionState.Disconnected, reason);
        if (_shouldRun && _options.AutoReconnect)
        {
            _ = Task.Run(ReconnectLoopAsync);
        }
    }

    private async Task ReconnectLoopAsync()
    {
        // only one reconnect loop runs at a time
        if (Interlocked.Exchange(ref _reconnecting, 1) != 0)
        {
            return;
        }

        try
        {
            var delay = _options.InitialReconnectDelay;
            while (_shouldRun)
            {
                var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
                await Task.Delay(delay + jitter).ConfigureAwait(false);
                if (!_shouldRun)
                {
                    return;
                }

                try
                {
                    await ConnectOnceAsync(CancellationToken.None).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Reconnect attempt failed: {Reason}", ex.Message);
                    delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, _options.MaxReconnectDelay.Ticks));
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _reconnecting, 0);
        }
    }

    private async Task TearDownAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await TearDownPumpAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task TearDownPumpAsync()
    {
        var pump = _pump;
        if (pump is not null)
        {
            pump.MessageReceived -= OnPumpMessage;
            pump.Closed -= OnPumpClosed;
            try
            {
                await pump.CloseAsync(WebSocketCloseStatus.NormalClosure, "client closing", TimeSpan.FromSeconds(3))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error while closing pump");
            }

            await pump.DisposeAsync().ConfigureAwait(false);
            _pump = null;
        }

        _socket = null;
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
