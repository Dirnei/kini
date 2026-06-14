namespace Kini.Api.WellKnown;

/// <summary>
/// z-base-32 encoder. The OpenPGP WKD spec hashes the local-part of an
/// email (lowercased) with SHA-1 and z-base-32-encodes the 20-byte result
/// into a 32-character URL path segment.
///
/// Alphabet: "ybndrfg8ejkmcpqxot1uwisza345h769" (see RFC 6189 / human-oriented
/// base-32 from Phil Zimmermann's work).
/// </summary>
public static class ZBase32
{
    private const string Alphabet = "ybndrfg8ejkmcpqxot1uwisza345h769";

    public static string Encode(ReadOnlySpan<byte> input)
    {
        if (input.IsEmpty) return string.Empty;

        var capacity = (input.Length * 8 + 4) / 5;
        var output = new System.Text.StringBuilder(capacity);

        int bits = 0;
        int bitsRemaining = 0;

        foreach (var b in input)
        {
            bits = (bits << 8) | b;
            bitsRemaining += 8;
            while (bitsRemaining >= 5)
            {
                bitsRemaining -= 5;
                output.Append(Alphabet[(bits >> bitsRemaining) & 0x1f]);
            }
        }
        if (bitsRemaining > 0)
        {
            output.Append(Alphabet[(bits << (5 - bitsRemaining)) & 0x1f]);
        }
        return output.ToString();
    }
}
