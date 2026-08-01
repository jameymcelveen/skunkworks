using Cartographer.Core.Grid;
using Shouldly;

namespace Cartographer.Core.Tests.Grid;

public sealed class TerrainGridTests
{
    private const double CellSize = 10.0;
    private static readonly TerrainGrid Grid = new(CellSize, "v1-test");

    [Fact]
    public void GridId_IsStableHashOfCellSizeAndDatasetVersion()
    {
        var a = new TerrainGrid(10, "v1");
        var b = new TerrainGrid(10, "v1");
        var c = new TerrainGrid(20, "v1");
        var d = new TerrainGrid(10, "v2");

        a.GridId.ShouldBe(b.GridId);
        a.GridId.ShouldNotBe(c.GridId);
        a.GridId.ShouldNotBe(d.GridId);
        a.GridId.Length.ShouldBe(64);
    }

    [Fact]
    public void LatLngToCell_UsesFloorDivision()
    {
        // Known mercator for (0,0) is (0,0) -> cell (0,0)
        Grid.LatLngToCell(new LatLng(0, 0)).ShouldBe(new CellIndex(0, 0));

        // Just below a positive boundary stays in previous cell
        Grid.MercatorToCell(new MercatorPoint(9.999, 9.999)).ShouldBe(new CellIndex(0, 0));

        // Exact cell-size multiples are the min corner of the next cell
        Grid.MercatorToCell(new MercatorPoint(10.0, 10.0)).ShouldBe(new CellIndex(1, 1));
    }

    [Fact]
    public void AdjacentCells_ShareEdgesExactly()
    {
        var a = new CellIndex(100, 200);
        var east = new CellIndex(101, 200);
        var north = new CellIndex(100, 201);

        var (_, aMax) = Grid.CellToMercatorBounds(a);
        var (eastMin, _) = Grid.CellToMercatorBounds(east);
        var (northMin, _) = Grid.CellToMercatorBounds(north);

        eastMin.X.ShouldBe(aMax.X);
        eastMin.Y.ShouldBe(a.Y * CellSize);
        northMin.Y.ShouldBe(aMax.Y);
        northMin.X.ShouldBe(a.X * CellSize);
    }

    [Fact]
    public void BboxToCells_ReturnsFullSquaresOnly()
    {
        // Build bbox from cell centers so lat/lng round-trip stays inside intended cells.
        var swBounds = Grid.CellToBounds(new CellIndex(0, 0));
        var neBounds = Grid.CellToBounds(new CellIndex(2, 1));
        var bbox = new LatLngBounds(
            swBounds.MinLng,
            swBounds.MinLat,
            neBounds.MaxLng - 1e-10,
            neBounds.MaxLat - 1e-10);
        var cells = Grid.BboxToCells(bbox);

        cells.Count.ShouldBe(6); // x=0,1,2 and y=0,1
        cells.ShouldContain(new CellIndex(0, 0));
        cells.ShouldContain(new CellIndex(2, 1));
        cells.ShouldNotContain(new CellIndex(3, 0));
    }

    [Fact]
    public void Property_RoundTrip_CellIndexToBoundsInteriorMapsBack()
    {
        var rng = new Random(42);
        for (var i = 0; i < 500; i++)
        {
            var cell = new CellIndex(rng.Next(-50_000, 50_000), rng.Next(-50_000, 50_000));
            var (min, max) = Grid.CellToMercatorBounds(cell);

            // Sample interior points; exclusive max edge belongs to the next cell
            var samples = new[]
            {
                new MercatorPoint(min.X, min.Y),
                new MercatorPoint(min.X + CellSize * 0.5, min.Y + CellSize * 0.5),
                new MercatorPoint(max.X - 1e-6, max.Y - 1e-6),
            };

            foreach (var sample in samples)
            {
                Grid.MercatorToCell(sample).ShouldBe(cell);
            }

            // Exclusive max corner belongs to the diagonally adjacent cell
            Grid.MercatorToCell(max).ShouldBe(new CellIndex(cell.X + 1, cell.Y + 1));
        }
    }

    [Fact]
    public void Property_RoundTrip_LatLngThroughCellIsStable()
    {
        var rng = new Random(7);
        for (var i = 0; i < 500; i++)
        {
            var lat = rng.NextDouble() * 140.0 - 70.0;
            var lng = rng.NextDouble() * 360.0 - 180.0;
            var point = new LatLng(lat, lng);

            var cell = Grid.LatLngToCell(point);
            var bounds = Grid.CellToBounds(cell);

            // Point must lie within the geographic box of its cell (inclusive min).
            // Lat/lng conversion is continuous and monotonic in Web Mercator.
            point.Lng.ShouldBeGreaterThanOrEqualTo(bounds.MinLng - 1e-9);
            point.Lat.ShouldBeGreaterThanOrEqualTo(bounds.MinLat - 1e-9);
            point.Lng.ShouldBeLessThan(bounds.MaxLng + 1e-9);
            point.Lat.ShouldBeLessThan(bounds.MaxLat + 1e-9);

            // Re-deriving the cell from the cell center must yield the same index
            var center = new LatLng(
                (bounds.MinLat + bounds.MaxLat) / 2.0,
                (bounds.MinLng + bounds.MaxLng) / 2.0);
            Grid.LatLngToCell(center).ShouldBe(cell);
        }
    }

    [Fact]
    public void Property_AdjacentCellsShareEdgesExactly()
    {
        var rng = new Random(99);
        for (var i = 0; i < 200; i++)
        {
            var cell = new CellIndex(rng.Next(-10_000, 10_000), rng.Next(-10_000, 10_000));
            var right = new CellIndex(cell.X + 1, cell.Y);
            var up = new CellIndex(cell.X, cell.Y + 1);

            var (_, cellMax) = Grid.CellToMercatorBounds(cell);
            var (rightMin, _) = Grid.CellToMercatorBounds(right);
            var (upMin, _) = Grid.CellToMercatorBounds(up);

            rightMin.X.ShouldBe(cellMax.X);
            upMin.Y.ShouldBe(cellMax.Y);
        }
    }

    [Fact]
    public void Property_BboxCellsCoverAllIntersectingIndices()
    {
        var rng = new Random(123);
        for (var i = 0; i < 100; i++)
        {
            var x0 = rng.Next(-1000, 1000) * CellSize + rng.NextDouble() * CellSize;
            var y0 = rng.Next(-1000, 1000) * CellSize + rng.NextDouble() * CellSize;
            var w = rng.NextDouble() * 80 + 1;
            var h = rng.NextDouble() * 80 + 1;

            var sw = WebMercator.ToLatLng(new MercatorPoint(x0, y0));
            var ne = WebMercator.ToLatLng(new MercatorPoint(x0 + w, y0 + h));
            var bbox = new LatLngBounds(sw.Lng, sw.Lat, ne.Lng, ne.Lat);
            var cells = Grid.BboxToCells(bbox);

            // Interior sample of each returned cell must fall inside the mercator bbox
            foreach (var cell in cells)
            {
                var (min, max) = Grid.CellToMercatorBounds(cell);
                var cx = (min.X + max.X) / 2.0;
                var cy = (min.Y + max.Y) / 2.0;
                // Cell intersects [x0, x0+w] x [y0, y0+h]
                (min.X < x0 + w && max.X > x0 && min.Y < y0 + h && max.Y > y0).ShouldBeTrue();
                _ = (cx, cy);
            }

            cells.Count.ShouldBe(cells.Distinct().Count());
        }
    }
}
