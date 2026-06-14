using System.Security.Cryptography;

namespace Kini.Api.Authentication.Sessions;

public static class TokenGenerator
{
    // 32 bytes of CSPRNG randomness → ~256 bits of entropy.
    // Encoded as URL-safe base64 (no padding) to keep it copy-paste friendly.
    public static (string Plaintext, string Hash) Issue()
    {
        Span<byte> raw = stackalloc byte[32];
        RandomNumberGenerator.Fill(raw);
        var plaintext = Base64UrlEncoder.Encode(raw);
        return (plaintext, HashOf(plaintext));
    }

    public static string HashOf(string plaintext)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(plaintext), hash);
        return Base64UrlEncoder.Encode(hash);
    }
}

internal static class Base64UrlEncoder
{
    public static string Encode(ReadOnlySpan<byte> bytes)
    {
        var s = Convert.ToBase64String(bytes);
        return s.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
