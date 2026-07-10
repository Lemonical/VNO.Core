using System;
using VNO.Core.Networking;
using VNO.Core.Protocol;
using Xunit;

namespace VNO.Core.Tests;

/// <summary>
/// Guards the shared production Master endpoint and application compatibility version.
/// </summary>
public sealed class MasterServerEndpointTests
{
    [Fact]
    public void Public_master_endpoint_uses_digitalocean_wss()
    {
        Assert.Equal("vno-master-rjrun.ondigitalocean.app", MasterServerEndpoint.Host);
        Assert.Equal(443, MasterServerEndpoint.Port);
        Assert.Equal(Transport.WebSocket, MasterServerEndpoint.Transport);
        Assert.True(MasterServerEndpoint.UseTls);
    }

    [Fact]
    public void Application_version_is_a_valid_system_version()
    {
        Assert.True(Version.TryParse(ProtocolConstants.ApplicationVersion, out _));
    }
}
