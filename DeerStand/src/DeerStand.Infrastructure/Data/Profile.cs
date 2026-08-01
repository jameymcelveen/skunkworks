namespace DeerStand.Infrastructure.Data;

/// <summary>App user profile. Id maps to the Zitadel subject claim.</summary>
public sealed class Profile
{
    public required string Id { get; set; }
    public required string FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<ClubMember> Memberships { get; set; } = [];
    public ICollection<Club> OwnedClubs { get; set; } = [];
}
