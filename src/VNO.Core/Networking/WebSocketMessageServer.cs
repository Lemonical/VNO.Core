using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VNO.Core.Protocol;

namespace VNO.Core.Networking;

/// <summary>
/// A WebSocket server that hosts many message sessions over a self hosted Kestrel
/// </summary>
/// <remarks>
/// Satisfies the same <see cref="IMessageServer"/> contract as <see cref="TcpMessageServer"/>,
/// so the AS and the game host swap to it with only DI and config changes. A WebSocket handshake
/// is an HTTP upgrade, so this owns a minimal Kestrel host that serves three routes: a cheap
/// liveness probe, a readiness probe App Platform gates deploys on, and the upgrade itself.
/// Session ids, the registry, and the dispatcher above are untouched. Handshake gating (banned
/// address, required subprotocol) happens before any command runs
/// </remarks>
public sealed class WebSocketMessageServer : IMessageServer
{
    private readonly ILogger<WebSocketMessageServer> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly WebSocketTransportOptions _options;
    private readonly ConcurrentDictionary<string, WebSocketSession> _sessions = new();

    private WebApplication? _app;
    private int _nextSessionId;

    /// <summary>
    /// Creates the server with a logger factory and optional transport options
    /// </summary>
    public WebSocketMessageServer(ILoggerFactory loggerFactory, WebSocketTransportOptions? options = null)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<WebSocketMessageServer>();
        _options = options ?? new WebSocketTransportOptions();
    }

    /// <inheritdoc />
    public bool IsListening => _app is not null;

    /// <summary>
    /// The port Kestrel actually bound, resolved after start
    /// </summary>
    /// <remarks>
    /// When started on port 0 the OS assigns an ephemeral port. Tests read it back here, and
    /// the value is stable for the lifetime of the listener. Returns 0 before start
    /// </remarks>
    public int BoundPort { get; private set; }

    /// <inheritdoc />
    public int SessionCount => _sessions.Count;

    /// <inheritdoc />
    public event EventHandler<SessionEventArgs>? SessionConnected;

    /// <inheritdoc />
    public event EventHandler<SessionEventArgs>? SessionDisconnected;

    /// <inheritdoc />
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// Optional handshake gate, return true to reject the upgrade from a banned address
    /// </summary>
    /// <remarks>
    /// The concrete registration wires this to the app's ban registry so a banned peer is
    /// refused at the HTTP layer, before a session or any command exists. It is async so a
    /// registry backed by a database is not blocked on a request thread. The generic
    /// interface stays transport agnostic, so this lives on the concrete type
    /// </remarks>
    public Func<string, CancellationToken, ValueTask<bool>>? IsAddressBanned { get; set; }

    /// <summary>
    /// Optional readiness probe, return true when dependencies are reachable
    /// </summary>
    /// <remarks>
    /// App Platform gates a new deploy on <c>/ready</c>. The app wires this to a real check,
    /// for example a database ping. When unset, readiness follows the listener being up
    /// </remarks>
    public Func<CancellationToken, Task<bool>>? ReadinessProbe { get; set; }

    /// <inheritdoc />
    public async Task StartAsync(int port, CancellationToken cancellationToken = default)
    {
        if (IsListening)
        {
            return;
        }

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.ListenAnyIP(port));

        // reuse the host logging rather than standing up a second logger stack
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(_loggerFactory);

        var app = builder.Build();
        app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = _options.KeepAliveInterval });

        app.MapGet(ProtocolConstants.HealthPath, () => Results.Ok("ok"));
        app.MapGet(ProtocolConstants.ReadyPath, (CancellationToken ct) => HandleReadinessAsync(ct));
        app.Map(_options.Path, HandleUpgradeAsync);

        await app.StartAsync(cancellationToken).ConfigureAwait(false);
        _app = app;
        BoundPort = ResolveBoundPort(app, port);
        _logger.LogInformation("WebSocket listener bound on port {Port} at {Path}", BoundPort, _options.Path);
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        var app = _app;
        if (app is null)
        {
            return;
        }

        _app = null;

        // stop accepting upgrades first, then drain every session with a bounded deadline
        try
        {
            await app.StopAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error stopping Kestrel");
        }

        foreach (var session in _sessions.Values)
        {
            await session.DisposeAsync().ConfigureAwait(false);
        }

        _sessions.Clear();
        await app.DisposeAsync().ConfigureAwait(false);
        _logger.LogInformation("WebSocket listener stopped");
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
            await session.SendAsync(message, cancellationToken).ConfigureAwait(false);
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

    private static int ResolveBoundPort(WebApplication app, int requestedPort)
    {
        if (requestedPort != 0)
        {
            return requestedPort;
        }

        // on port 0 the OS assigns the port, recover it from the bound server addresses
        var addresses = app.Services
            .GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
            .Features
            .Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();

        foreach (var address in addresses?.Addresses ?? System.Array.Empty<string>())
        {
            if (System.Uri.TryCreate(address, System.UriKind.Absolute, out var uri) && uri.Port > 0)
            {
                return uri.Port;
            }
        }

        return requestedPort;
    }

    private async Task<IResult> HandleReadinessAsync(CancellationToken cancellationToken)
    {
        if (!IsListening)
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var probe = ReadinessProbe;
        if (probe is null)
        {
            return Results.Ok("ready");
        }

        try
        {
            var ready = await probe(cancellationToken).ConfigureAwait(false);
            return ready ? Results.Ok("ready") : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Readiness probe threw");
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    private async Task HandleUpgradeAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var address = ResolveRemoteAddress(context);

        // connect time version gate, reject an upgrade that does not request our subprotocol
        if (!context.WebSockets.WebSocketRequestedProtocols.Contains(_options.Subprotocol))
        {
            _logger.LogWarning("Rejecting upgrade from {Address}, missing subprotocol {Protocol}",
                address, _options.Subprotocol);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var banGate = IsAddressBanned;
        if (banGate is not null && await banGate(address, context.RequestAborted).ConfigureAwait(false))
        {
            _logger.LogWarning("Rejecting upgrade from banned address {Address}", address);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync(_options.Subprotocol).ConfigureAwait(false);
        var id = Interlocked.Increment(ref _nextSessionId)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);

        var session = new WebSocketSession(id, socket, address, _options, _loggerFactory.CreateLogger<WebSocketSession>());
        session.MessageReceived += OnSessionMessage;
        session.Closed += OnSessionClosed;
        _sessions[id] = session;
        session.Start();

        _logger.LogInformation("Session {Id} connected from {Address}", id, address);
        SessionConnected?.Invoke(this, new SessionEventArgs(id, address));

        // keep the request alive for the socket's lifetime, ASP.NET closes it when we return
        await session.Completion.ConfigureAwait(false);
    }

    private static string ResolveRemoteAddress(HttpContext context)
    {
        // behind App Platform ingress the peer address arrives in a forwarded header, prefer
        // the first hop when present, otherwise the direct connection address
        var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded.Split(',', 2)[0].Trim();
            if (first.Length > 0)
            {
                return first;
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
    }

    private void OnSessionMessage(WebSocketSession session, NetworkMessage message) =>
        MessageReceived?.Invoke(this, new MessageReceivedEventArgs(session.Id, message));

    private void OnSessionClosed(WebSocketSession session)
    {
        if (_sessions.TryRemove(session.Id, out _))
        {
            _logger.LogInformation("Session {Id} disconnected", session.Id);
            SessionDisconnected?.Invoke(this, new SessionEventArgs(session.Id, session.RemoteAddress));
        }
    }
}
