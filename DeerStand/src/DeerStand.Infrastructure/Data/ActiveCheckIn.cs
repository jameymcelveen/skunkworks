namespace DeerStand.Infrastructure.Data;

/// <summary>
/// Live occupancy of a stand. StandId is unique: one hunter per stand.
/// Row is deleted on checkout.
/// </summary>
public sealed class ActiveCheckIn
{
    public Guid StandId { get; set; }
    public required string ProfileId { get; set; }
    public DateTimeOffset CheckedInAt { get; set; }

    public Stand? Stand { get; set; }
    public Profile? Profile { get; set; }
}
