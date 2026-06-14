using System.Text.RegularExpressions;

namespace Kini.Api.Authentication.Identities;

/// <summary>
/// Validation rules for the public username — appears in URLs like
/// <c>/{username}.keys</c>, <c>/{username}.gpg</c>, and on cards in the UI.
/// Globally unique. Distinct from the identity's email (which WKD requires
/// and which the user can't change without breaking GPG discovery).
/// </summary>
public static class Username
{
    /// <summary>Lowercase letters, digits, hyphens. Must start and end with alnum. 2-32 chars.</summary>
    public static readonly Regex Pattern = new(
        @"^[a-z0-9](?:[a-z0-9-]{0,30}[a-z0-9])?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Routes / system identifiers we don't want hijacked. Comparison is case-insensitive
    /// because usernames are stored lowercase anyway.
    /// </summary>
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "administrator", "root", "support", "help", "billing", "system",
        "www", "api", "app", "auth", "v1", "v2", "static", "assets", "public",
        "sign-in", "sign-up", "signin", "signup", "sign-out", "signout", "login", "logout",
        "well-known", ".well-known", "healthz", "ready", "metrics",
        "kini", "docs", "blog", "status",
    };

    /// <summary>Returns true and the canonicalized username, or false with a reason.</summary>
    public static bool TryNormalize(string? raw, out string canonical, out string? reason)
    {
        canonical = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            reason = "Username is required.";
            return false;
        }

        var trimmed = raw.Trim().ToLowerInvariant();
        if (trimmed.Length < 2)
        {
            reason = "Username must be at least 2 characters.";
            return false;
        }
        if (trimmed.Length > 32)
        {
            reason = "Username must be at most 32 characters.";
            return false;
        }
        if (!Pattern.IsMatch(trimmed))
        {
            reason = "Lowercase letters, digits, and hyphens only — no leading or trailing hyphen.";
            return false;
        }
        if (Reserved.Contains(trimmed))
        {
            reason = "That username is reserved.";
            return false;
        }

        canonical = trimmed;
        reason = null;
        return true;
    }
}
