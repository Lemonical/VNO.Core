using VNO.Core;
using VNO.Core.Protocol;
using Xunit;

namespace VNO.Core.Tests;

/// <summary>
/// Covers parsing recorded replay lines back into messages
/// </summary>
public sealed class ReplayLineTests
{
    [Fact]
    public void Parses_an_in_character_line()
    {
        var message = ReplayLine.Parse("IC|Phoenix|Objection!");

        Assert.NotNull(message);
        Assert.Equal(MessageType.InCharacter, message!.Type);
        Assert.Equal("Phoenix", message.GetArgument(0));
        Assert.Equal("Objection!", message.GetArgument(1));
    }

    [Fact]
    public void Parses_an_out_of_character_line()
    {
        var message = ReplayLine.Parse("OOC|hello room");

        Assert.NotNull(message);
        Assert.Equal(MessageType.OutOfCharacter, message!.Type);
        Assert.Equal("hello room", message.GetArgument(0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NONSENSE")]
    [InlineData("IC|onlyname")]
    [InlineData("OOC")]
    public void Rejects_unrecognized_lines(string line)
    {
        Assert.Null(ReplayLine.Parse(line));
    }
}
