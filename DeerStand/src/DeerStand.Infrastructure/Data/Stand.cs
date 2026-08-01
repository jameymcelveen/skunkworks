namespace DeerStand.Infrastructure.Data;

/// <summary>Physical hunting stand pin on the club map.</summary>
public sealed class Stand
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public required string Name { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Club? Club { get; set; }
    public ActiveCheckIn? ActiveCheckIn { get; set; }
    public ICollection<CheckInHistory> CheckInHistory { get; set; } = [];
    public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
}
