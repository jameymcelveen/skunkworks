using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cartographer.Core.Grid;

/// <summary>
/// Fixed-size Web Mercator grid. Cell size is meters; grid identity is a hash
/// of (cell size, dataset version) so changing size creates a new grid.
/// </summary>
public sealed class TerrainGrid
{
    public const double DefaultCellSizeMeters = 10.0;

    public TerrainGrid(double cellSizeMeters, string datasetVersion)
    {
        if (cellSizeMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSizeMeters), "Cell size must be positive.");
        }

        if (string.IsNullOrWhiteSpace(datasetVersion))
        {
            throw new ArgumentException("Dataset version is required.", nameof(datasetVersion));
        }

        CellSizeMeters = cellSizeMeters;
        DatasetVersion = datasetVersion.Trim();
        GridId = ComputeGridId(CellSizeMeters, DatasetVersion);
    }

    public double CellSizeMeters { get; }

    public string DatasetVersion { get; }

    public string GridId { get; }

    /// <summary>
    /// Maps a geographic point to the cell that contains it.
    /// </summary>
    public CellIndex LatLngToCell(LatLng latLng)
    {
        var m = WebMercator.FromLatLng(latLng);
        return MercatorToCell(m);
    }

    /// <summary>
    /// Maps Web Mercator meters to the cell that contains the point.
    /// </summary>
    public CellIndex MercatorToCell(MercatorPoint point)
    {
        var x = FloorDiv(point.X, CellSizeMeters);
        var y = FloorDiv(point.Y, CellSizeMeters);
        return new CellIndex(x, y);
    }

    /// <summary>
    /// Returns the geographic bounds of a cell as a LatLng box.
    /// The max edge is exclusive in Mercator space (shared with the next cell).
    /// </summary>
    public LatLngBounds CellToBounds(CellIndex cell)
    {
        var min = new MercatorPoint(cell.X * CellSizeMeters, cell.Y * CellSizeMeters);
        var max = new MercatorPoint((cell.X + 1) * CellSizeMeters, (cell.Y + 1) * CellSizeMeters);
        var sw = WebMercator.ToLatLng(min);
        var ne = WebMercator.ToLatLng(max);
        return new LatLngBounds(sw.Lng, sw.Lat, ne.Lng, ne.Lat);
    }

    /// <summary>
    /// Returns Mercator bounds for a cell. Max edges are exclusive.
    /// </summary>
    public (MercatorPoint Min, MercatorPoint Max) CellToMercatorBounds(CellIndex cell)
    {
        var min = new MercatorPoint(cell.X * CellSizeMeters, cell.Y * CellSizeMeters);
        var max = new MercatorPoint((cell.X + 1) * CellSizeMeters, (cell.Y + 1) * CellSizeMeters);
        return (min, max);
    }

    /// <summary>
    /// Returns the number of cells that intersect the geographic bbox without allocating.
    /// </summary>
    public long CountCellsInBbox(LatLngBounds bbox)
    {
        if (!bbox.IsValid)
        {
            throw new ArgumentException("Bounding box is invalid.", nameof(bbox));
        }

        var (minX, maxX, minY, maxY) = BboxCellRange(bbox);
        return (maxX - minX + 1) * (maxY - minY + 1);
    }

    /// <summary>
    /// Enumerates every cell that intersects the geographic bbox.
    /// Partial cells are never returned; indices are full squares.
    /// </summary>
    public IReadOnlyList<CellIndex> BboxToCells(LatLngBounds bbox)
    {
        var (minX, maxX, minY, maxY) = BboxCellRange(bbox);
        var countLong = (maxX - minX + 1) * (maxY - minY + 1);
        if (countLong > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(bbox), "Bounding box covers too many cells.");
        }

        var count = (int)countLong;
        var cells = new List<CellIndex>(count);
        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                cells.Add(new CellIndex(x, y));
            }
        }

        return cells;
    }

    private (long MinX, long MaxX, long MinY, long MaxY) BboxCellRange(LatLngBounds bbox)
    {
        if (!bbox.IsValid)
        {
            throw new ArgumentException("Bounding box is invalid.", nameof(bbox));
        }

        var sw = WebMercator.FromLatLng(new LatLng(bbox.MinLat, bbox.MinLng));
        var ne = WebMercator.FromLatLng(new LatLng(bbox.MaxLat, bbox.MaxLng));

        var minX = FloorDiv(sw.X, CellSizeMeters);
        var maxX = FloorDiv(ne.X, CellSizeMeters);
        var minY = FloorDiv(sw.Y, CellSizeMeters);
        var maxY = FloorDiv(ne.Y, CellSizeMeters);

        // If the NE corner lands exactly on a cell boundary, that cell's min
        // edge is outside the exclusive max of the previous cell, so include it
        // only when the point is strictly inside or the bbox has positive extent
        // that touches the next cell. Floor already includes the cell containing
        // the max point when the point is not on a boundary; when it is exactly
        // on a boundary, FloorDiv(ne) yields the next cell's index which would
        // have zero interior overlap. Shrink when the max sits exactly on a grid line.
        if (IsExactMultiple(ne.X, CellSizeMeters) && maxX > minX)
        {
            maxX--;
        }

        if (IsExactMultiple(ne.Y, CellSizeMeters) && maxY > minY)
        {
            maxY--;
        }

        return (minX, maxX, minY, maxY);
    }

    /// <summary>
    /// Stable grid identity: lowercase hex SHA-256 of "cellSize|datasetVersion".
    /// </summary>
    public static string ComputeGridId(double cellSizeMeters, string datasetVersion)
    {
        var payload = string.Create(
            CultureInfo.InvariantCulture,
            $"{cellSizeMeters:R}|{datasetVersion.Trim()}");
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static long FloorDiv(double value, double divisor)
    {
        return (long)Math.Floor(value / divisor);
    }

    private static bool IsExactMultiple(double value, double divisor)
    {
        var q = value / divisor;
        return Math.Abs(q - Math.Round(q)) < 1e-9;
    }
}
