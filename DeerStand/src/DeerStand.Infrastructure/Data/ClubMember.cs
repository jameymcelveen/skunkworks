namespace DeerStand.Infrastructure.Data;

/// <summary>Membership of a profile in a club. Unique on (ClubId, ProfileId).</summary>
public sealed class ClubMember
{
    public Guid ClubId { get; set; }
    public required string ProfileId { get; set; }
    public required string Role { get; set; }

    public Club? Club { get; set; }
    public Profile? Profile { get; set; }
}
