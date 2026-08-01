namespace DeerStand.Core;

/// <summary>Club membership role. Stored as a lowercase string in the database.</summary>
public static class ClubRoles
{
    public const string Owner = "owner";
    public const string Member = "member";

    public static bool IsValid(string role) =>
        role is Owner or Member;
}
