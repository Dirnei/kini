using System.Diagnostics;
using System.Text;

namespace Kini.Api.Keys;

/// <summary>
/// Parsed OpenPGP public key: the original armored block, its V4
/// fingerprint, and the user-IDs bound to it. We don't reimplement
/// OpenPGP — we shell out to <c>gpg --show-keys --with-colons</c> and
/// trust its parsing.
/// </summary>
public sealed record GpgPublicKey(string Armored, string Fingerprint, string Algorithm, string[] Uids, byte[] Binary)
{
    /// <summary>
    /// Validate + parse an armored OpenPGP public key block.
    /// Throws <see cref="FormatException"/> on anything gpg refuses.
    /// </summary>
    public static async Task<GpgPublicKey> ParseAsync(string armored, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(armored))
            throw new FormatException("GPG public key is empty.");
        if (!armored.Contains("-----BEGIN PGP PUBLIC KEY BLOCK-----"))
            throw new FormatException("Not an ASCII-armored OpenPGP public key block.");

        // Use a throwaway GNUPGHOME so the process never touches a real keyring.
        var work = Path.Combine(Path.GetTempPath(), $"kini-gpg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(work);
        try
        {
            var (fingerprint, algorithm, uids) = await ShowKey(work, armored, ct);
            var binary = Dearmor(armored);
            return new GpgPublicKey(armored.Trim(), fingerprint, algorithm, uids, binary);
        }
        finally
        {
            try { Directory.Delete(work, recursive: true); } catch { /* best-effort */ }
        }
    }

    private static async Task<(string Fingerprint, string Algorithm, string[] Uids)> ShowKey(
        string gpgHome, string armored, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "gpg",
            ArgumentList = { "--no-default-keyring", "--show-keys", "--with-colons", "--with-fingerprint" },
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            Environment = { ["GNUPGHOME"] = gpgHome },
        };

        using var proc = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start gpg.");

        await proc.StandardInput.WriteAsync(armored.AsMemory(), ct);
        proc.StandardInput.Close();

        var stdout = await proc.StandardOutput.ReadToEndAsync(ct);
        var stderr = await proc.StandardError.ReadToEndAsync(ct);
        await proc.WaitForExitAsync(ct);

        if (proc.ExitCode != 0)
            throw new FormatException($"gpg rejected the key: {stderr.Trim()}");

        // Parse the colons format. Reference: doc/DETAILS in gnupg source.
        //   pub:..:bits:algo:keyid:...
        //   fpr:::::::::FINGERPRINT:
        //   uid:......:utf8(name <email>):
        string? fingerprint = null;
        string algorithm = "";
        var uids = new List<string>();

        foreach (var line in stdout.Split('\n'))
        {
            var fields = line.Split(':');
            if (fields.Length < 2) continue;
            switch (fields[0])
            {
                case "pub" when fields.Length >= 4:
                    algorithm = AlgorithmName(fields[3]);
                    break;
                case "fpr" when fingerprint is null && fields.Length >= 10:
                    fingerprint = fields[9];
                    break;
                case "uid" when fields.Length >= 10:
                    var uidValue = fields[9];
                    if (!string.IsNullOrWhiteSpace(uidValue))
                        uids.Add(UnescapeUid(uidValue));
                    break;
            }
        }

        if (string.IsNullOrEmpty(fingerprint))
            throw new FormatException("gpg produced no fingerprint for the key.");

        return ($"OPENPGP4:{fingerprint}", algorithm, uids.ToArray());
    }

    /// <summary>
    /// Strip the ASCII armor and return the raw OpenPGP transferable public key
    /// packets. Used to serve the WKD binary form without re-running gpg per
    /// request.
    /// </summary>
    public static byte[] Dearmor(string armored)
    {
        var lines = armored.Replace("\r\n", "\n").Split('\n');
        var body = new StringBuilder();
        var inHeaders = false;
        var inBody = false;
        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            if (line.StartsWith("-----BEGIN PGP "))
            {
                inHeaders = true;
                continue;
            }
            if (line.StartsWith("-----END PGP "))
                break;

            if (inHeaders && !inBody)
            {
                // First blank line ends the header block; the rest is the
                // base64-encoded packet stream.
                if (line.Length == 0) { inBody = true; continue; }
                // Header lines look like "Version: GnuPG ..." — skip them.
                if (line.Contains(':')) continue;
                // Some armored keys have no header block — body starts immediately.
                inBody = true;
            }
            if (!inBody) continue;
            if (line.StartsWith("=")) continue; // CRC-24 footer
            body.Append(line);
        }

        try
        {
            return Convert.FromBase64String(body.ToString());
        }
        catch (FormatException ex)
        {
            throw new FormatException("Armored OpenPGP body is not valid base64.", ex);
        }
    }

    private static string AlgorithmName(string algoCode) => algoCode switch
    {
        "1"  => "rsa",
        "17" => "dsa",
        "18" => "ecdh",
        "19" => "ecdsa",
        "22" => "eddsa",
        "23" => "ed25519",
        _    => "openpgp",
    };

    // gpg --with-colons escapes ':' as '\x3a', '\\' as '\x5c', etc. Best-effort.
    private static string UnescapeUid(string s)
    {
        if (!s.Contains('\\')) return s;
        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '\\' && i + 3 < s.Length && s[i + 1] == 'x')
            {
                if (int.TryParse(s.AsSpan(i + 2, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
                {
                    sb.Append((char)b);
                    i += 3;
                    continue;
                }
            }
            sb.Append(s[i]);
        }
        return sb.ToString();
    }
}
