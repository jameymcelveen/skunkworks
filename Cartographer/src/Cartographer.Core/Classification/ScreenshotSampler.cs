using Cartographer.Core.Grid;

namespace Cartographer.Core.Classification;

/// <summary>
/// Maps a screenshot covering a mercator bbox onto per-cell sample windows.
/// Assumes the image is axis-aligned Web Mercator (EPSG:3857).
/// </summary>
public sealed class ScreenshotSampler
{
    private readonly TerrainGrid _grid;
    private readonly int _pixelsPerCell;
    private readonly int _tolerance;

    public ScreenshotSampler(TerrainGrid grid, int pixelsPerCell = 8, int tolerance = SentinelPalette.DefaultTolerance)
    {
        if (pixelsPerCell < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelsPerCell));
        }

        _grid = grid;
        _pixelsPerCell = pixelsPerCell;
        _tolerance = tolerance;
    }

    public int PixelsPerCell => _pixelsPerCell;

    /// <summary>
    /// Computes the pixel size required to cover the cell bbox at the target resolution.
    /// </summary>
    public (int Width, int Height) ImageSizeForCells(int cellsWide, int cellsHigh)
        => (cellsWide * _pixelsPerCell, cellsHigh * _pixelsPerCell);

    /// <summary>
    /// Classifies every cell in [originX, originX+width) x [originY, originY+height)
    /// from a tightly packed RGB buffer matching <see cref="ImageSizeForCells"/>.
    /// </summary>
    public IReadOnlyList<(CellIndex Index, CellSample Sample)> ClassifyGrid(
        ReadOnlySpan<byte> rgbImage,
        int imageWidth,
        int imageHeight,
        long originX,
        long originY,
        int cellsWide,
        int cellsHigh)
    {
        var expectedW = cellsWide * _pixelsPerCell;
        var expectedH = cellsHigh * _pixelsPerCell;
        if (imageWidth != expectedW || imageHeight != expectedH)
        {
            throw new ArgumentException(
                $"Image size {imageWidth}x{imageHeight} does not match expected {expectedW}x{expectedH}.");
        }

        if (rgbImage.Length < imageWidth * imageHeight * 3)
        {
            throw new ArgumentException("RGB buffer is too small for the declared image size.");
        }

        var results = new List<(CellIndex, CellSample)>(cellsWide * cellsHigh);
        var block = new byte[_pixelsPerCell * _pixelsPerCell * 3];

        for (var cy = 0; cy < cellsHigh; cy++)
        {
            for (var cx = 0; cx < cellsWide; cx++)
            {
                var px0 = cx * _pixelsPerCell;
                // Image row 0 is top (north / higher mercator Y in our fitBounds usage).
                // Cell grid Y increases northward in Web Mercator, so invert rows.
                var imageRow0 = (cellsHigh - 1 - cy) * _pixelsPerCell;
                var i = 0;
                for (var py = 0; py < _pixelsPerCell; py++)
                {
                    var row = imageRow0 + py;
                    for (var px = 0; px < _pixelsPerCell; px++)
                    {
                        var src = ((row * imageWidth) + (px0 + px)) * 3;
                        block[i++] = rgbImage[src];
                        block[i++] = rgbImage[src + 1];
                        block[i++] = rgbImage[src + 2];
                    }
                }

                var sample = CellClassifier.ClassifyRgb(block, _pixelsPerCell * _pixelsPerCell, _tolerance);
                results.Add((new CellIndex(originX + cx, originY + cy), sample));
            }
        }

        return results;
    }
}
