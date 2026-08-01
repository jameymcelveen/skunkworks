namespace DeerStand.Infrastructure.Tenants;

/// <summary>
/// Per-request tenant subject and club membership set.
/// Hydrated by auth middleware (milestone 2). Used by EF query filters and the RLS interceptor.
/// </summary>
public interface ITenantContext
{
    /// <summary>Zitadel subject (maps to Profile.Id). Null when unauthenticated.</summary>
    string? ProfileId { get; set; }

    /// <summary>Club IDs the caller belongs to. Empty when unauthenticated.</summary>
    IReadOnlySet<Guid> ClubIds { get; set; }
}

public sealed class TenantContext : ITenantContext
{
    public string? ProfileId { get; set; }

    public IReadOnlySet<Guid> ClubIds { get; set; } = new HashSet<Guid>();
}
