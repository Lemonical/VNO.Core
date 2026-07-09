using System;
using System.Linq;
using VNO.Core.Protocol;
using Xunit;

namespace VNO.Core.Tests;

/// <summary>
/// Tests for the streaming message framer
/// </summary>
public sealed class MessageFramerTests
{
    [Fact]
    public void Two_messages_in_one_chunk_are_both_returned()
    {
        var framer = new MessageFramer();
        var wire = MessageCodec.Encode(new NetworkMessage(MessageType.OutOfCharacter, "one"))
                   + MessageCodec.Encode(new NetworkMessage(MessageType.OutOfCharacter, "two"));

        var messages = framer.Append(wire);

        Assert.Equal(2, messages.Count);
        Assert.Equal("one", messages[0].GetArgument(0));
        Assert.Equal("two", messages[1].GetArgument(0));
    }

    [Fact]
    public void A_message_split_across_chunks_is_joined()
    {
        var framer = new MessageFramer();
        var wire = MessageCodec.Encode(new NetworkMessage(MessageType.Notice, "split me"));
        var half = wire.Length / 2;

        var first = framer.Append(wire[..half]);
        var second = framer.Append(wire[half..]);

        Assert.Empty(first);
        Assert.Single(second);
        Assert.Equal("split me", second[0].GetArgument(0));
    }

    [Fact]
    public void An_escaped_terminator_does_not_end_a_message()
    {
        var framer = new MessageFramer();
        var wire = MessageCodec.Encode(new NetworkMessage(MessageType.Notice, "50% done"));

        var messages = framer.Append(wire);

        Assert.Single(messages);
        Assert.Equal("50% done", messages.Single().GetArgument(0));
    }

    [Fact]
    public void A_utf8_code_point_split_across_byte_chunks_is_preserved()
    {
        const string argument = "court\u2026";
        var framer = new MessageFramer();
        var wire = MessageCodec.EncodeBytes(new NetworkMessage(MessageType.Notice, argument));
        var split = Array.IndexOf(wire, (byte)0xE2) + 1;

        Assert.True(split > 0);

        var first = framer.Append(wire.AsSpan(0, split));
        var second = framer.Append(wire.AsSpan(split));

        Assert.Empty(first);
        Assert.Single(second);
        Assert.Equal(argument, second[0].GetArgument(0));
    }
}
