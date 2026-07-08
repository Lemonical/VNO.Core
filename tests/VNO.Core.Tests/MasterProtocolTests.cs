using VNO.Core.Protocol;
using Xunit;

namespace VNO.Core.Tests;

/// <summary>
/// Tests for the master service auth and listing verbs added to the protocol
/// </summary>
/// <remarks>
/// The wire strings are ported one to one from the legacy VNO master, these tests
/// guard against an accidental drift back to invented names
/// </remarks>
public sealed class MasterProtocolTests
{
    [Theory]
    [InlineData(MessageType.VersionCheck, "VER")]
    [InlineData(MessageType.ConnectionPrompt, "CV")]
    [InlineData(MessageType.VersionAccepted, "VEROK")]
    [InlineData(MessageType.VersionRejected, "VERPB")]
    [InlineData(MessageType.MasterLogin, "CO")]
    [InlineData(MessageType.LoginGranted, "VNAL")]
    [InlineData(MessageType.LoginDenied, "VNODE")]
    [InlineData(MessageType.MasterHeartbeat, "HB")]
    [InlineData(MessageType.CreateAccount, "CA")]
    [InlineData(MessageType.AccountCreated, "OKCR")]
    [InlineData(MessageType.AccountTaken, "NOKCR")]
    [InlineData(MessageType.AccountInvalid, "INVCHAR")]
    [InlineData(MessageType.RequestServers, "RPS")]
    [InlineData(MessageType.ServerEntry, "SDP")]
    [InlineData(MessageType.BadgeGrant, "BDGR")]
    [InlineData(MessageType.GameTokenRequest, "GTREQ")]
    [InlineData(MessageType.GameTokenIssued, "GTISS")]
    [InlineData(MessageType.GameTokenValidate, "GTVAL")]
    [InlineData(MessageType.GameTokenValidation, "GTRES")]
    [InlineData(MessageType.RegisterServer, "RequestPub")]
    [InlineData(MessageType.CheckIp, "CHIP")]
    [InlineData(MessageType.IpClear, "OKAY")]
    [InlineData(MessageType.IpBanned, "SUBAN")]
    public void Master_verb_maps_to_its_wire_header_both_ways(MessageType type, string header)
    {
        Assert.Equal(header, MessageHeaders.ToHeader(type));
        Assert.Equal(type, MessageHeaders.FromHeader(header));
    }

    [Fact]
    public void Address_and_account_bans_share_the_legacy_vnobd_string()
    {
        // the original master sent VNOBD for both a banned address and a banned account
        Assert.Equal("VNOBD", MessageHeaders.ToHeader(MessageType.AddressBanned));
        Assert.Equal("VNOBD", MessageHeaders.ToHeader(MessageType.AccountBanned));
    }

    [Fact]
    public void Server_entry_round_trips_all_listing_fields()
    {
        // a server row carries index, name, ip, port, description, content url, flag
        var original = new NetworkMessage(
            MessageType.ServerEntry, "0", "Courtroom", "10.0.0.5", "16789", "Best room", "http://x/y", "no");

        var wire = MessageCodec.Encode(original);
        var parsed = MessageCodec.Decode(wire.TrimEnd(ProtocolConstants.MessageTerminator));

        Assert.Equal(MessageType.ServerEntry, parsed.Type);
        Assert.Equal("SDP", parsed.Header);
        Assert.Equal("Courtroom", parsed.GetArgument(1));
        Assert.Equal("16789", parsed.GetArgument(3));
        Assert.Equal("no", parsed.GetArgument(6));
    }

    [Fact]
    public void Badge_grant_round_trips_the_name_and_badge_id()
    {
        // the master pushes one of these per badge holder at login, the client keys the
        // stage badge on the shown name in argument 0
        var original = new NetworkMessage(MessageType.BadgeGrant, "Noevain", "champ");

        var wire = MessageCodec.Encode(original);
        var parsed = MessageCodec.Decode(wire.TrimEnd(ProtocolConstants.MessageTerminator));

        Assert.Equal(MessageType.BadgeGrant, parsed.Type);
        Assert.Equal("BDGR", parsed.Header);
        Assert.Equal("Noevain", parsed.GetArgument(0));
        Assert.Equal("champ", parsed.GetArgument(1));
    }
}
