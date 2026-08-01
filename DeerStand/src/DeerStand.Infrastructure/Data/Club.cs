namespace DeerStand.Infrastructure.Data;

/// <summary>Hunting club. The tenant boundary for all club-scoped data.</summary>
public sealed class Club
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string InviteCode { get; set; }
    public required string OwnerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Profile? Owner { get; set; }
    public ICollection<ClubMember> Members { get; set; } = [];
    public ICollection<Stand> Stands { get; set; } = [];
    public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
}
