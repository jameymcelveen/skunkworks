namespace DeerStand.Infrastructure.Data;

/// <summary>
/// Append-only check-in history. Inserted once on checkout with both timestamps
/// (see ADR 0001). Never updated.
/// </summary>
public sealed class CheckInHistory
{
    public Guid Id { get; set; }
    public Guid StandId { get; set; }
    public required string ProfileId { get; set; }
    public DateTimeOffset CheckedInAt { get; set; }
    public DateTimeOffset CheckedOutAt { get; set; }

    public Stand? Stand { get; set; }
    public Profile? Profile { get; set; }
}
