namespace Cartographer.Core.Classification;

/// <summary>
/// Sentinel RGB colors used by the MapLibre style and the sampler.
/// </summary>
public static class SentinelPalette
{
    public const int DefaultTolerance = 24;

    public static readonly IReadOnlyDictionary<TerrainClass, Rgb> Colors =
        new Dictionary<TerrainClass, Rgb>
        {
            [TerrainClass.Water] = new(0x00, 0x00, 0xFF),
            [TerrainClass.Swamp] = new(0x00, 0xFF, 0xFF),
            [TerrainClass.Trees] = new(0x00, 0x77, 0x00),
            [TerrainClass.Grass] = new(0x00, 0xFF, 0x00),
            [TerrainClass.Field] = new(0xAA, 0xFF, 0x00),
            [TerrainClass.Sand] = new(0xFF, 0xFF, 0x00),
            [TerrainClass.DirtRoad] = new(0xAA, 0x55, 0x00),
            [TerrainClass.PavedRoad] = new(0xFF, 0x00, 0xFF),
            [TerrainClass.Structure] = new(0xFF, 0x00, 0x00),
            [TerrainClass.Dirt] = new(0x88, 0x55, 0x33),
        };

    /// <summary>
    /// Snaps an RGB sample to the nearest sentinel class.
    /// Exact match preferred; otherwise nearest within <paramref name="tolerance"/>.
    /// Returns Unknown when outside tolerance (sampling failure).
    /// </summary>
    public static TerrainClass Snap(byte r, byte g, byte b, int tolerance = DefaultTolerance)
    {
        TerrainClass best = TerrainClass.Unknown;
        var bestDist = int.MaxValue;

        foreach (var (cls, color) in Colors)
        {
            var dr = r - color.R;
            var dg = g - color.G;
            var db = b - color.B;
            var dist = dr * dr + dg * dg + db * db;
            if (dist == 0)
            {
                return cls;
            }

            if (dist < bestDist)
            {
                bestDist = dist;
                best = cls;
            }
        }

        return bestDist <= tolerance * tolerance ? best : TerrainClass.Unknown;
    }
}

/// <summary>8-bit RGB triple.</summary>
public readonly record struct Rgb(byte R, byte G, byte B);
