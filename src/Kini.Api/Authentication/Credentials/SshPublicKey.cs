using System.Security.Cryptography;

namespace Kini.Api.Authentication.Credentials;

/// <summary>
/// Parses an authorized_keys-style line and derives algorithm + SHA256 fingerprint.
/// Format: <c>&lt;algorithm&gt; &lt;base64-blob&gt; [comment]</c>
/// </summary>
public sealed record SshPublicKey(string Algorithm, string Blob, string? Comment, string Fingerprint, string Canonical)
{
    public static SshPublicKey Parse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            throw new FormatException("SSH public key is empty.");

        var trimmed = line.Trim();
        var firstSpace = trimmed.IndexOf(' ');
        if (firstSpace <= 0) throw new FormatException("SSH public key missing algorithm.");

        var algorithm = trimmed[..firstSpace];
        var rest = trimmed[(firstSpace + 1)..].TrimStart();

        var secondSpace = rest.IndexOf(' ');
        var blob = secondSpace < 0 ? rest : rest[..secondSpace];
        var comment = secondSpace < 0 ? null : rest[(secondSpace + 1)..].Trim();

        if (!IsKnownAlgorithm(algorithm))
            throw new FormatException($"Unknown SSH key algorithm '{algorithm}'.");

        byte[] decoded;
        try { decoded = Convert.FromBase64String(blob); }
        catch (FormatException) { throw new FormatException("SSH public key blob is not valid base64."); }

        // SHA256 fingerprint = SHA256:<base64-no-padding(sha256(blob))>
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(decoded, hash);
        var fingerprint = "SHA256:" + Convert.ToBase64String(hash).TrimEnd('=');

        // Canonical form drops any trailing comment; keeps the algorithm + blob only.
        var canonical = $"{algorithm} {blob}" + (string.IsNullOrEmpty(comment) ? "" : $" {comment}");

        return new SshPublicKey(algorithm, blob, comment, fingerprint, canonical);
    }

    private static bool IsKnownAlgorithm(string alg) => alg is
        "ssh-ed25519" or "ssh-rsa" or "ecdsa-sha2-nistp256" or "ecdsa-sha2-nistp384" or
        "ecdsa-sha2-nistp521" or "ssh-dss" or "sk-ssh-ed25519@openssh.com" or
        "sk-ecdsa-sha2-nistp256@openssh.com";
}
