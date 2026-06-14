using System.Diagnostics;

namespace Kini.Api.Authentication.Credentials;

/// <summary>
/// Verifies an SSH signature by shelling out to <c>ssh-keygen -Y verify</c>.
/// Requires <c>openssh-client</c> to be present in the runtime container.
/// </summary>
public sealed class SshSignatureVerifier
{
    public const string Namespace = "kini";

    private readonly ILogger<SshSignatureVerifier> _log;

    public SshSignatureVerifier(ILogger<SshSignatureVerifier> log)
    {
        _log = log;
    }

    public async Task<bool> Verify(
        string payload,
        string signatureArmored,
        string allowedIdentity,
        string allowedPublicKey,
        CancellationToken ct = default)
    {
        var workDir = Path.Combine(Path.GetTempPath(), $"kini-verify-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workDir);

        var allowedSignersPath = Path.Combine(workDir, "allowed_signers");
        var sigPath = Path.Combine(workDir, "sig");

        try
        {
            // allowed_signers line: "<email> <algorithm-and-pubkey-blob>"
            // Default encoding (UTF-8 *without* BOM) is critical here — ssh-keygen
            // refuses files that start with a UTF-8 BOM as "invalid format."
            await File.WriteAllTextAsync(
                allowedSignersPath,
                $"{allowedIdentity} {allowedPublicKey}\n",
                ct);

            // Append a trailing newline so the BEGIN/END SSH SIGNATURE block is
            // line-terminated, which some ssh-keygen versions require.
            var sigToWrite = signatureArmored.EndsWith('\n') ? signatureArmored : signatureArmored + "\n";
            await File.WriteAllTextAsync(sigPath, sigToWrite, ct);

            var startInfo = new ProcessStartInfo
            {
                FileName = "ssh-keygen",
                ArgumentList = {
                    "-Y", "verify",
                    "-n", Namespace,
                    "-I", allowedIdentity,
                    "-f", allowedSignersPath,
                    "-s", sigPath,
                },
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using var proc = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start ssh-keygen.");

            await proc.StandardInput.WriteAsync(payload.AsMemory(), ct);
            proc.StandardInput.Close();

            var stdout = await proc.StandardOutput.ReadToEndAsync(ct);
            var stderr = await proc.StandardError.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);

            if (proc.ExitCode == 0 && stdout.Contains($"Good \"{Namespace}\" signature"))
            {
                return true;
            }

            _log.LogInformation(
                "ssh-keygen verify rejected signature (exit {Exit}): stdout={Stdout} stderr={Stderr}",
                proc.ExitCode, stdout.Trim(), stderr.Trim());
            return false;
        }
        finally
        {
            try { Directory.Delete(workDir, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }
}
