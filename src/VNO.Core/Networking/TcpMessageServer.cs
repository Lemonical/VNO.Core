using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VNO.Core.Protocol;

namespace VNO.Core.Networking;

/// <summary>
/// A TCP server that hosts many message sessions at once
/// </summary>
/// <remarks>
/// This replaces the legacy TServerSocket. It accepts peers on a background loop,
/// wraps each in a <see cref="TcpSession"/>, and surfaces messages and lifecycle
/// events. Session storage is a concurrent dictionary so reads and writes from
/// different threads are safe
/// </remarks>
public sealed class TcpMessageServer : IMessageServer
{
    private readonly ILogger<TcpMessageServer> _logger;
    private readonly ConcurrentDictionary<string, TcpSession> _sessions = new();
    private readonly int _maximumMessageBytes;

    private TcpListener? _listener;
    private CancellationTokenSource? _acceptCts;
    private Task? _acceptLoop;
    private int _nextSessionId;

    /// <summary>
    /// Creates the server with a logger
    /// </summary>
    public TcpMessageServer(
        ILogger<TcpMessageServer> logger,
        int maximumMessageBytes = ProtocolConstants.MaxMessageBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumMessageBytes);
        _logger = logger;
        _maximumMessageBytes = maximumMessageBytes;
    }

    /// <inheritdoc />
    public bool IsListening => _listener is not null;

    /// <inheritdoc />
    public int SessionCount => _sessions.Count;

    /// <inheritdoc />
    public event EventHandler<SessionEventArgs>? SessionConnected;

    /// <inheritdoc />
    public event EventHandler<SessionEventArgs>? SessionDisconnected;

    /// <inheritdoc />
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <inheritdoc />
    public Task StartAsync(int port, CancellationToken cancellationToken = default)
    {
        if (IsListening)
        {
            return Task.CompletedTask;
        }

        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        _acceptCts = new CancellationTokenSource();
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_acceptCts.Token), cancellationToken);
        _logger.LogInformation("Listening on port {Port}", port);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        if (!IsListening)
        {
            return;
        }

        try
        {
            _acceptCts?.Cancel();
            _listener?.Stop();
            if (_acceptLoop is not null)
            {
                await _acceptLoop.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // expected during shutdown
        }

        foreach (var session in _sessions.Values)
        {
            await session.DisposeAsync().ConfigureAwait(false);
        }

        _sessions.Clear();
        _acceptCts?.Dispose();
        _acceptCts = null;
        _listener = null;
        _acceptLoop = null;
        _logger.LogInformation("Server stopped");
    }

    /// <inheritdoc />
    public async Task SendToAsync(string sessionId, NetworkMessage message, CancellationToken cancellationToken = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            await session.SendAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task BroadcastAsync(NetworkMessage message, CancellationToken cancellationToken = default)
    {
        foreach (var session in _sessions.Values)
        {
            try
            {
                await session.SendAsync(message, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Dropping slow TCP session {Id}", session.Id);
                await DisconnectAsync(session.Id).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            await session.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public string GetRemoteAddress(string sessionId) =>
        _sessions.TryGetValue(sessionId, out var session) ? session.RemoteAddress : string.Empty;

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _listener is not null)
            {
                var tcp = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                var id = Interlocked.Increment(ref _nextSessionId)
                    .ToString(System.Globalization.CultureInfo.InvariantCulture);
                var session = new TcpSession(id, tcp, _maximumMessageBytes);
                session.MessageReceived += OnSessionMessage;
                session.Closed += OnSessionClosed;
                _sessions[id] = session;
                session.Start();

                _logger.LogInformation("Session {Id} connected from {Address}", id, session.RemoteAddress);
                SessionConnected?.Invoke(this, new SessionEventArgs(id, session.RemoteAddress));
            }
        }
        catch (OperationCanceledException)
        {
            // expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Accept loop failed");
        }
    }

    private void OnSessionMessage(TcpSession session, NetworkMessage message) =>
        MessageReceived?.Invoke(this, new MessageReceivedEventArgs(session.Id, message));

    private void OnSessionClosed(TcpSession session)
    {
        if (_sessions.TryRemove(session.Id, out _))
        {
            _logger.LogInformation("Session {Id} disconnected", session.Id);
            SessionDisconnected?.Invoke(this, new SessionEventArgs(session.Id, session.RemoteAddress));
            _ = DisposeClosedSessionAsync(session);
        }
    }

    private async Task DisposeClosedSessionAsync(TcpSession session)
    {
        try
        {
            await session.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Disposing closed session {Id} failed", session.Id);
        }
    }
}
