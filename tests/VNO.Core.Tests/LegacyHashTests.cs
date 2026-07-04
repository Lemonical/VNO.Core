using VNO.Core.Protocol;
using Xunit;

namespace VNO.Core.Tests;

/// <summary>
/// Covers the MD5 helper used for legacy wire compatible password hashing
/// </summary>
public sealed class LegacyHashTests
{
    [Theory]
    [InlineData("", "d41d8cd98f00b204e9800998ecf8427e")]
    [InlineData("Password", "dc647eb65e6711e155375218212b3964")]
    [InlineData("porttest", "46698516efc5f33cb014c14c83cb1a96")]
    public void Produces_lowercase_hex_md5(string input, string expected)
    {
        Assert.Equal(expected, LegacyHash.Md5Hex(input));
    }

    [Fact]
    public void Same_input_is_stable()
    {
        Assert.Equal(LegacyHash.Md5Hex("abc"), LegacyHash.Md5Hex("abc"));
    }
}
