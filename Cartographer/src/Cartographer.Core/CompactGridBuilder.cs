using Cartographer.Core.Grid;

namespace Cartographer.Core;

/// <summary>
/// Builds the compact row-major discovery payload.
/// </summary>
public static class CompactGridBuilder
{
    public static CompactCellGrid Build(
        string gridId,
        long originX,
        long originY,
        int width,
        int height,
        IReadOnlyDictionary<CellIndex, CellRecord> known,
        DateTimeOffset now)
    {
        var classes = new string?[width * height];
        var pending = false;
        var stale = false;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = new CellIndex(originX + x, originY + y);
                var i = y * width + x;
                if (!known.TryGetValue(index, out var record))
                {
                    classes[i] = null;
                    pending = true;
                    continue;
                }

                classes[i] = TerrainClassCodec.ToWire(record.Class);
                if (record.ExpiresAt <= now)
                {
                    stale = true;
                }
            }
        }

        return new CompactCellGrid(gridId, originX, originY, width, height, classes, pending, stale);
    }
}
