namespace Cartographer.Core.Classification;

/// <summary>
/// Majority-vote classifier over a rectangular pixel block.
/// </summary>
public static class CellClassifier
{
    /// <summary>
    /// Classifies a cell from tightly packed RGB pixels (3 bytes per pixel, row-major).
    /// </summary>
    public static CellSample ClassifyRgb(ReadOnlySpan<byte> rgb, int pixelCount, int tolerance = SentinelPalette.DefaultTolerance)
    {
        if (pixelCount <= 0 || rgb.Length < pixelCount * 3)
        {
            return new CellSample(TerrainClass.Unknown, null, 0f);
        }

        var counts = new Dictionary<TerrainClass, int>();
        for (var i = 0; i < pixelCount; i++)
        {
            var o = i * 3;
            var cls = SentinelPalette.Snap(rgb[o], rgb[o + 1], rgb[o + 2], tolerance);
            counts[cls] = counts.GetValueOrDefault(cls) + 1;
        }

        return FromCounts(counts, pixelCount);
    }

    /// <summary>
    /// Classifies from pre-snapped class votes.
    /// </summary>
    public static CellSample FromCounts(IReadOnlyDictionary<TerrainClass, int> counts, int totalPixels)
    {
        if (totalPixels <= 0 || counts.Count == 0)
        {
            return new CellSample(TerrainClass.Unknown, null, 0f);
        }

        TerrainClass? winner = null;
        TerrainClass? runnerUp = null;
        var winCount = -1;
        var runnerCount = -1;

        foreach (var (cls, count) in counts)
        {
            if (count > winCount)
            {
                runnerUp = winner;
                runnerCount = winCount;
                winner = cls;
                winCount = count;
            }
            else if (count > runnerCount)
            {
                runnerUp = cls;
                runnerCount = count;
            }
        }

        var confidence = winCount / (float)totalPixels;
        return new CellSample(winner!.Value, runnerUp, confidence);
    }
}
