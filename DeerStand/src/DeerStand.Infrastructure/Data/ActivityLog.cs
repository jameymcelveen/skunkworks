namespace DeerStand.Infrastructure.Data;

/// <summary>Club activity entry: sighting, harvest, bait site, or photo.</summary>
public sealed class ActivityLog
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public required string ProfileId { get; set; }
    public Guid? StandId { get; set; }
    public required string LogType { get; set; }
    public string? Details { get; set; }
    public string? ImageUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Club? Club { get; set; }
    public Profile? Profile { get; set; }
    public Stand? Stand { get; set; }
}
