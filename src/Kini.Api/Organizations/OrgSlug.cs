using System.Text.RegularExpressions;

namespace Kini.Api.Organizations;

/// <summary>
/// URL-safe org identifier. Globally unique, lowercase alnum + hyphen,
/// 2–32 chars. <c>Slug</c> intentionally forbids dots so it cannot collide
/// with the dotted-DNS shape of <see cref="Organization.PrimaryDomain"/> —
/// both fields share the same URL path namespace but live in disjoint
/// string spaces.
/// </summary>
public static class OrgSlug
{
    public static readonly Regex Pattern = new(
        @"^[a-z0-9](?:[a-z0-9-]{0,30}[a-z0-9])?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "app", "api", "v1", "v2",
        "sign-in", "sign-up", "signin", "signup", "sign-out", "signout", "login", "logout",
        "well-known", ".well-known", "healthz", "ready", "metrics",
        "assets", "static", "public",
        "auth", "kini", "docs", "blog", "status", "admin",
        "support", "help", "billing",
    };

    public static bool TryNormalize(string? raw, out string canonical, out string? reason)
    {
        canonical = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            reason = "Org slug is required.";
            return false;
        }
        var trimmed = raw.Trim().ToLowerInvariant();
        if (trimmed.Contains('.'))
        {
            reason = "Slug may not contain dots — use the primaryDomain field for DNS-style identifiers.";
            return false;
        }
        if (!Pattern.IsMatch(trimmed))
        {
            reason = "Lowercase letters, digits, and hyphens only; 2–32 chars; no leading or trailing hyphen.";
            return false;
        }
        if (Reserved.Contains(trimmed))
        {
            reason = "That slug is reserved.";
            return false;
        }
        canonical = trimmed;
        reason = null;
        return true;
    }
}
