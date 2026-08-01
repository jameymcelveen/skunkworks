using Shouldly;

namespace Cartographer.Core.Tests;

public sealed class TerrainClassCodecTests
{
    [Theory]
    [InlineData(TerrainClass.Water, "water")]
    [InlineData(TerrainClass.Swamp, "swamp")]
    [InlineData(TerrainClass.DirtRoad, "dirt_road")]
    [InlineData(TerrainClass.PavedRoad, "paved_road")]
    [InlineData(TerrainClass.Unknown, "unknown")]
    public void RoundTrips(TerrainClass value, string wire)
    {
        TerrainClassCodec.ToWire(value).ShouldBe(wire);
        TerrainClassCodec.Parse(wire).ShouldBe(value);
    }
}
