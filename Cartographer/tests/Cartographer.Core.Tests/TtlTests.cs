using Cartographer.Core;
using Shouldly;

namespace Cartographer.Core.Tests;

public sealed class TtlTests
{
    [Fact]
    public void ComputeExpiry_AppliesUpToTenPercentJitter()
    {
        var sampled = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var ttl = TimeSpan.FromDays(100);
        var rng = new Random(1);
        var expiry = Ttl.ComputeExpiry(sampled, ttl, rng);
        var delta = expiry - sampled;
        delta.ShouldBeGreaterThanOrEqualTo(ttl);
        delta.ShouldBeLessThanOrEqualTo(ttl + TimeSpan.FromTicks((long)(ttl.Ticks * 0.10)));
    }
}
