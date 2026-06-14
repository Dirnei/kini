namespace Kini.Api.Authentication.Identities;

/// <summary>
/// Role within an organization. v0 has exactly two: Owner (godlike — created the
/// org, can manage members, can publish keys on their behalf) and Member
/// (manages only their own credentials, sessions, and keys).
/// </summary>
public static class IdentityRole
{
    public const string Owner = "owner";
    public const string Member = "member";

    public static bool IsKnown(string? value) => value is Owner or Member;
}
