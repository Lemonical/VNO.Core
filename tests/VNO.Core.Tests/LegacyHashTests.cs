using VNO.Core.Protocol;
using Xunit;

namespace VNO.Core.Tests;

/// <summary>
/// Covers the MD5 helper used for legacy wire compatible password hashing
/// </summary>
public sealed class LegacyHashTests
{
    [Theory]
    [InlineData("", "D41D8CD98F00B204E9800998ECF8427E")]
    [InlineData("Password", "DC647EB65E6711E155375218212B3964")]
    [InlineData("porttest", "46698516EFC5F33CB014C14C83CB1A96")]
    public void Produces_uppercase_hex_md5(string input, string expected)
    {
        Assert.Equal(expected, LegacyHash.Md5Hex(input));
    }

    [Fact]
    public void Same_input_is_stable()
    {
        Assert.Equal(LegacyHash.Md5Hex("abc"), LegacyHash.Md5Hex("abc"));
    }

    [Theory]
    [InlineData("Password", "DC647EB65E6711E155375218212B3964")]
    [InlineData("dc647eb65e6711e155375218212b3964", "DC647EB65E6711E155375218212B3964")]
    [InlineData("DC647EB65E6711E155375218212B3964", "DC647EB65E6711E155375218212B3964")]
    public void Wire_credential_does_not_double_hash_existing_digests(string input, string expected)
    {
        Assert.Equal(expected, LegacyHash.ToWireCredential(input));
    }
}
