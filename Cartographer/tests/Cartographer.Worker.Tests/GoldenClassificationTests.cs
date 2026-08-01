using System.Text.Json;
using Cartographer.Core;
using Cartographer.Core.Classification;
using Cartographer.Core.Grid;
using Cartographer.Worker;
using Cartographer.Worker.Rendering;
using Shouldly;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Cartographer.Worker.Tests;

public sealed class GoldenClassificationTests
{
    private const int PixelsPerCell = 8;
    private const int CellsWide = 4;
    private const int CellsHigh = 3;

    /// <summary>
    /// Fixture layout (origin at SW; image Y flipped so north is top):
    /// Row y=2 (image top):    water | water | trees | trees
    /// Row y=1:                grass | grass | dirt  | paved_road
    /// Row y=0 (image bottom): sand  | field | structure | dirt_road
    /// </summary>
    private static readonly TerrainClass[,] Layout =
    {
        { TerrainClass.Sand, TerrainClass.Field, TerrainClass.Structure, TerrainClass.DirtRoad },
        { TerrainClass.Grass, TerrainClass.Grass, TerrainClass.Dirt, TerrainClass.PavedRoad },
        { TerrainClass.Water, TerrainClass.Water, TerrainClass.Trees, TerrainClass.Trees },
    };

    private static readonly string[] ExpectedClasses =
    [
        "sand", "field", "structure", "dirt_road",
        "grass", "grass", "dirt", "paved_road",
        "water", "water", "trees", "trees",
    ];

    [Fact]
    public void GoldenFixture_ClassifiesToExactSnapshot()
    {
        var fixturesDir = LocateFixturesDir();
        var pngPath = Path.Combine(fixturesDir, "golden-sentinel.png");
        var snapshotPath = Path.Combine(fixturesDir, "golden-grid.json");

        WriteFixturePng(pngPath);
        File.WriteAllText(snapshotPath, JsonSerializer.Serialize(ExpectedClasses) + "\n");

        var png = File.ReadAllBytes(pngPath);
        var expected = JsonSerializer.Deserialize<string[]>(File.ReadAllText(snapshotPath))!;
        expected.ShouldBe(ExpectedClasses);

        var grid = new TerrainGrid(10, "golden");
        var results = RenderBatchProcessor.ClassifyPng(
            png, grid, originX: 0, originY: 0, CellsWide, CellsHigh, PixelsPerCell);

        results.Count.ShouldBe(ExpectedClasses.Length);
        var actual = results.Select(r => TerrainClassCodec.ToWire(r.Sample.Class)).ToArray();
        actual.ShouldBe(expected);

        foreach (var (_, sample) in results)
        {
            sample.Confidence.ShouldBe(1.0f);
            sample.Class.ShouldNotBe(TerrainClass.Unknown);
        }
    }

    [Fact]
    public void Snap_PrefersExactMatchAndFallsBackWithinTolerance()
    {
        SentinelPalette.Snap(0, 0, 255).ShouldBe(TerrainClass.Water);
        SentinelPalette.Snap(5, 5, 250).ShouldBe(TerrainClass.Water);
        SentinelPalette.Snap(128, 128, 128).ShouldBe(TerrainClass.Unknown);
    }

    [Fact]
    public void AssertLegalTileOrigin_BlocksCommercialHosts()
    {
        Should.Throw<InvalidOperationException>(() =>
            MapRenderer.AssertLegalTileOrigin("https://api.mapbox.com/v1/tiles"));
        Should.Throw<InvalidOperationException>(() =>
            MapRenderer.AssertLegalTileOrigin("https://maps.googleapis.com/maps/vt"));
        Should.NotThrow(() =>
            MapRenderer.AssertLegalTileOrigin("http://127.0.0.1:8080/region.pmtiles"));
    }

    private static void WriteFixturePng(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var w = CellsWide * PixelsPerCell;
        var h = CellsHigh * PixelsPerCell;
        using var image = new Image<Rgba32>(w, h);

        for (var cy = 0; cy < CellsHigh; cy++)
        {
            for (var cx = 0; cx < CellsWide; cx++)
            {
                var color = SentinelPalette.Colors[Layout[cy, cx]];
                var pixel = new Rgba32(color.R, color.G, color.B, 255);
                var imageRow0 = (CellsHigh - 1 - cy) * PixelsPerCell;
                for (var py = 0; py < PixelsPerCell; py++)
                {
                    for (var px = 0; px < PixelsPerCell; px++)
                    {
                        image[cx * PixelsPerCell + px, imageRow0 + py] = pixel;
                    }
                }
            }
        }

        image.SaveAsPng(path);
    }

    private static string LocateFixturesDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.Name == "Cartographer.Worker.Tests")
            {
                var local = Path.Combine(dir.FullName, "Fixtures");
                Directory.CreateDirectory(local);
                return local;
            }

            var fromRepo = Path.Combine(dir.FullName, "tests", "Cartographer.Worker.Tests", "Fixtures");
            if (File.Exists(Path.Combine(dir.FullName, "Cartographer.sln")))
            {
                Directory.CreateDirectory(fromRepo);
                return fromRepo;
            }

            dir = dir.Parent;
        }

        var fallback = Path.Combine(AppContext.BaseDirectory, "Fixtures");
        Directory.CreateDirectory(fallback);
        return fallback;
    }
}
