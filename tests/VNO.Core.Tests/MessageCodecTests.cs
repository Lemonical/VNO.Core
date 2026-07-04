using VNO.Core.Protocol;
using Xunit;

namespace VNO.Core.Tests;

/// <summary>
/// Tests for the message encoder and decoder
/// </summary>
public sealed class MessageCodecTests
{
    [Fact]
    public void Encode_then_decode_round_trips_a_simple_message()
    {
        var original = new NetworkMessage(MessageType.OutOfCharacter, "hello world");

        var wire = MessageCodec.Encode(original);
        // strip the terminator before decoding the body
        var body = wire.TrimEnd(ProtocolConstants.MessageTerminator);
        var parsed = MessageCodec.Decode(body);

        Assert.Equal(MessageType.OutOfCharacter, parsed.Type);
        Assert.Equal("hello world", parsed.GetArgument(0));
    }

    [Fact]
    public void Delimiter_and_terminator_inside_an_argument_survive_a_round_trip()
    {
        // an argument that holds both special characters must not break framing
        var tricky = "a#b%c\\d";
        var original = new NetworkMessage(MessageType.Notice, tricky);

        var wire = MessageCodec.Encode(original);
        var body = wire.TrimEnd(ProtocolConstants.MessageTerminator);
        var parsed = MessageCodec.Decode(body);

        Assert.Equal(tricky, parsed.GetArgument(0));
    }

    [Fact]
    public void Unknown_header_decodes_as_unknown_type()
    {
        var parsed = MessageCodec.Decode("ZZZ#one#two");

        Assert.Equal(MessageType.Unknown, parsed.Type);
        Assert.Equal("ZZZ", parsed.Header);
        Assert.Equal("one", parsed.GetArgument(0));
        Assert.Equal("two", parsed.GetArgument(1));
    }

    [Fact]
    public void Missing_argument_returns_empty_string()
    {
        var message = NetworkMessage.Create(MessageType.Heartbeat);

        Assert.Equal(string.Empty, message.GetArgument(0));
    }
}
