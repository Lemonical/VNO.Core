using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VNO.Core.Models;
using VNO.Core.Networking;
using VNO.Core.Protocol;
using Xunit;

namespace VNO.Core.Tests;

/// <summary>
/// Loopback integration tests for the WebSocket transport pair
/// </summary>
/// <remarks>
/// The server binds port 0 so the OS assigns a free port, read back through BoundPort. This
/// avoids the fixed port flakiness the migration plan calls out
/// </remarks>
public sealed class WebSocketTransportTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static WebSocketTransportOptions ClientOptions() => new()
    {
        UseTls = false,
        AutoReconnect = false,
    };

    [Fact]
    public async Task Client_and_server_round_trip_a_message()
    {
        await using var server = new WebSocketMessageServer(NullLoggerFactory.Instance);
        var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.SessionConnected += (_, _) => connected.TrySetResult();

        // echo every inbound message straight back to its sender
        server.MessageReceived += (_, e) => _ = server.SendToAsync(e.SessionId, e.Message);

        await server.StartAsync(0);

        await using var client = new WebSocketMessageClient(NullLoggerFactory.Instance.CreateLogger<WebSocketMessageClient>(), ClientOptions());
        var received = new TaskCompletionSource<NetworkMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.MessageReceived += (_, e) => received.TrySetResult(e.Message);

        await client.ConnectAsync("127.0.0.1", server.BoundPort);
        await connected.Task.WaitAsync(Timeout);

        await client.SendAsync(new NetworkMessage(MessageType.OutOfCharacter, "ping", "pong"));

        var echoed = await received.Task.WaitAsync(Timeout);
        Assert.Equal(MessageType.OutOfCharacter, echoed.Type);
        Assert.Equal("ping", echoed.GetArgument(0));
        Assert.Equal("pong", echoed.GetArgument(1));
    }

    [Fact]
    public async Task Server_broadcast_reaches_a_connected_client()
    {
        await using var server = new WebSocketMessageServer(NullLoggerFactory.Instance);
        var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.SessionConnected += (_, _) => connected.TrySetResult();
        await server.StartAsync(0);

        await using var client = new WebSocketMessageClient(NullLoggerFactory.Instance.CreateLogger<WebSocketMessageClient>(), ClientOptions());
        var received = new TaskCompletionSource<NetworkMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.MessageReceived += (_, e) => received.TrySetResult(e.Message);

        await client.ConnectAsync("127.0.0.1", server.BoundPort);
        await connected.Task.WaitAsync(Timeout);

        await server.BroadcastAsync(new NetworkMessage(MessageType.Notice, "hello all"));

        var got = await received.Task.WaitAsync(Timeout);
        Assert.Equal(MessageType.Notice, got.Type);
        Assert.Equal("hello all", got.GetArgument(0));
    }

    [Fact]
    public async Task Disconnect_raises_session_disconnected_on_the_server()
    {
        await using var server = new WebSocketMessageServer(NullLoggerFactory.Instance);
        var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.SessionConnected += (_, _) => connected.TrySetResult();
        server.SessionDisconnected += (_, _) => disconnected.TrySetResult();
        await server.StartAsync(0);

        var client = new WebSocketMessageClient(NullLoggerFactory.Instance.CreateLogger<WebSocketMessageClient>(), ClientOptions());
        await client.ConnectAsync("127.0.0.1", server.BoundPort);
        await connected.Task.WaitAsync(Timeout);

        await client.DisconnectAsync();
        await disconnected.Task.WaitAsync(Timeout);

        Assert.Equal(0, server.SessionCount);
    }

    [Fact]
    public async Task Handshake_is_rejected_when_the_subprotocol_is_missing()
    {
        await using var server = new WebSocketMessageServer(NullLoggerFactory.Instance);
        await server.StartAsync(0);

        using var raw = new ClientWebSocket();
        // deliberately omit the vno.v2 subprotocol, the server must refuse the upgrade
        var uri = new Uri($"ws://127.0.0.1:{server.BoundPort}{ProtocolConstants.WebSocketPath}");

        await Assert.ThrowsAsync<WebSocketException>(() => raw.ConnectAsync(uri, CancellationToken.None));
    }

    [Fact]
    public async Task Banned_address_is_refused_at_the_handshake()
    {
        await using var server = new WebSocketMessageServer(NullLoggerFactory.Instance)
        {
            IsAddressBanned = (_, _) => ValueTask.FromResult(true),
        };
        await server.StartAsync(0);

        await using var client = new WebSocketMessageClient(NullLoggerFactory.Instance.CreateLogger<WebSocketMessageClient>(), ClientOptions());

        await Assert.ThrowsAnyAsync<Exception>(() => client.ConnectAsync("127.0.0.1", server.BoundPort));
        Assert.Equal(ConnectionState.Faulted, client.State);
    }

    [Fact]
    public async Task Auto_reconnect_reestablishes_the_link_after_the_server_returns()
    {
        var options = new WebSocketTransportOptions
        {
            UseTls = false,
            AutoReconnect = true,
            InitialReconnectDelay = TimeSpan.FromMilliseconds(100),
            MaxReconnectDelay = TimeSpan.FromMilliseconds(500),
        };

        var server = new WebSocketMessageServer(NullLoggerFactory.Instance);
        await server.StartAsync(0);
        var port = server.BoundPort;

        await using var client = new WebSocketMessageClient(NullLoggerFactory.Instance.CreateLogger<WebSocketMessageClient>(), options);
        await client.ConnectAsync("127.0.0.1", port);
        Assert.Equal(ConnectionState.Connected, client.State);

        // drop the server, the client should notice and begin retrying
        await server.StopAsync();
        await server.DisposeAsync();

        // bring a fresh server up on the same port and expect the client to reconnect
        var replacement = new WebSocketMessageServer(NullLoggerFactory.Instance);
        var reconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        replacement.SessionConnected += (_, _) => reconnected.TrySetResult();
        await StartOnPortWithRetryAsync(replacement, port);

        await reconnected.Task.WaitAsync(Timeout);
        await replacement.DisposeAsync();
    }

    private static async Task StartOnPortWithRetryAsync(WebSocketMessageServer server, int port)
    {
        // the freed port may linger briefly, retry a few times before giving up
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await server.StartAsync(port);
                return;
            }
            catch when (attempt < 10)
            {
                await Task.Delay(100);
            }
        }
    }
}
