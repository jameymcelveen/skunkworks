namespace Cartographer.Core;

/// <summary>
/// TTL helpers with stampede-breaking jitter.
/// </summary>
public static class Ttl
{
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromDays(90);

    /// <summary>
    /// expires_at = sampled_at + ttl with up to 10 percent random jitter.
    /// </summary>
    public static DateTimeOffset ComputeExpiry(DateTimeOffset sampledAt, TimeSpan ttl, Random? random = null)
    {
        random ??= Random.Shared;
        var jitterFraction = random.NextDouble() * 0.10;
        var jittered = ttl + TimeSpan.FromTicks((long)(ttl.Ticks * jitterFraction));
        return sampledAt + jittered;
    }
}
