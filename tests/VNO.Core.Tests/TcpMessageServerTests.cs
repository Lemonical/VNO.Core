using System;
using Microsoft.Extensions.Logging.Abstractions;
using VNO.Core.Networking;
using Xunit;

namespace VNO.Core.Tests;

public sealed class TcpMessageServerTests
{
    [Fact]
    public void Invalid_message_limit_is_rejected_during_construction()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TcpMessageServer(NullLogger<TcpMessageServer>.Instance, 0));
    }
}
