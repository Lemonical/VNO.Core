using System;
using System.Security.Cryptography;
using System.Text;

namespace VNO.Core.Protocol;

/// <summary>
/// Hashing used by the legacy protocol
/// </summary>
/// <remarks>
/// The legacy client hashed account passwords with MD5 before sending them to
/// the AS (the CO and CA commands), and the AS only ever compared the strings
/// it received. MD5 is kept solely for wire compatibility with that behavior,
/// it is not used as a general purpose hash
/// </remarks>
public static class LegacyHash
{
    /// <summary>
    /// Returns the uppercase hex MD5 digest of the UTF-8 bytes of the text
    /// </summary>
    public static string Md5Hex(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        // CA5351 is suppressed because the legacy wire format requires MD5
#pragma warning disable CA5351
        var digest = MD5.HashData(Encoding.UTF8.GetBytes(text));
#pragma warning restore CA5351
        return Convert.ToHexString(digest);
    }

    /// <summary>
    /// Converts plaintext or an existing MD5 digest to the canonical wire credential
    /// </summary>
    public static string ToWireCredential(string passwordOrDigest)
    {
        ArgumentNullException.ThrowIfNull(passwordOrDigest);
        return IsMd5Hex(passwordOrDigest)
            ? passwordOrDigest.ToUpperInvariant()
            : Md5Hex(passwordOrDigest);
    }

    private static bool IsMd5Hex(string value)
    {
        if (value.Length != 32)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (!Uri.IsHexDigit(character))
            {
                return false;
            }
        }

        return true;
    }
}
