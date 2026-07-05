using System;
using System.Linq;
using VNO.Core.Protocol;
using Xunit;

namespace VNO.Core.Tests;

/// <summary>
/// Tests for the WebSocket frame codec path and the decoder field caps
/// </summary>
public sealed class MessageCodecFrameTests
{
    [Fact]
    public void Frame_encode_then_decode_round_trips_a_message()
    {
        var original = new NetworkMessage(MessageType.OutOfCharacter, "hello", "world");

        var frame = MessageCodec.EncodeFrameBytes(original);
        var parsed = MessageCodec.DecodeFrame(frame);

        Assert.Equal(MessageType.OutOfCharacter, parsed.Type);
        Assert.Equal("hello", parsed.GetArgument(0));
        Assert.Equal("world", parsed.GetArgument(1));
    }

    [Fact]
    public void Frame_bytes_carry_no_terminator()
    {
        var frame = MessageCodec.EncodeFrameBytes(new NetworkMessage(MessageType.Notice, "x"));
        var text = ProtocolConstants.WireEncoding.GetString(frame);

        Assert.DoesNotContain(ProtocolConstants.MessageTerminator, text);
    }

    [Fact]
    public void Special_characters_survive_the_frame_round_trip()
    {
        var tricky = "a#b%c\\d";
        var frame = MessageCodec.EncodeFrameBytes(new NetworkMessage(MessageType.Notice, tricky));

        var parsed = MessageCodec.DecodeFrame(frame);

        Assert.Equal(tricky, parsed.GetArgument(0));
    }

    [Fact]
    public void Decode_rejects_a_frame_over_the_field_count_cap()
    {
        // a wall of delimiters would otherwise allocate one empty field per delimiter
        var flood = new string('#', 50);

        Assert.Throws<ProtocolFormatException>(() => MessageCodec.Decode(flood, maxFieldCount: 8, maxFieldLength: 1024));
    }

    [Fact]
    public void Decode_rejects_a_frame_over_the_field_length_cap()
    {
        var huge = new string('a', 200);

        Assert.Throws<ProtocolFormatException>(() => MessageCodec.Decode(huge, maxFieldCount: 8, maxFieldLength: 64));
    }

    [Fact]
    public void Decode_accepts_input_within_the_caps()
    {
        var parsed = MessageCodec.Decode("MC#one#two", maxFieldCount: 8, maxFieldLength: 64);

        Assert.Equal(3, parsed.Arguments.Count + 1);
        Assert.Equal("one", parsed.GetArgument(0));
    }

    [Fact]
    public void Frame_and_stream_encodings_differ_only_by_the_terminator()
    {
        var message = new NetworkMessage(MessageType.Notice, "abc", "def");

        var stream = ProtocolConstants.WireEncoding.GetString(MessageCodec.EncodeBytes(message));
        var frame = ProtocolConstants.WireEncoding.GetString(MessageCodec.EncodeFrameBytes(message));

        Assert.Equal(stream, frame + ProtocolConstants.MessageTerminator);
    }
}
