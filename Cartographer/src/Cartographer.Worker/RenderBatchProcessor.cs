using Cartographer.Core;
using Cartographer.Core.Classification;
using Cartographer.Core.Grid;
using Cartographer.Core.Storage;
using Cartographer.Worker.Rendering;
using Microsoft.Extensions.Options;

namespace Cartographer.Worker;

public sealed class GridOptions
{
    public const string SectionName = "Grid";

    public double CellSizeMeters { get; set; } = TerrainGrid.DefaultCellSizeMeters;

    public string DatasetVersion { get; set; } = "v1";

    public int TtlDays { get; set; } = 90;
}

/// <summary>
/// End-to-end: claim job, render bbox, classify, upsert cells.
/// </summary>
public sealed class RenderBatchProcessor
{
    private readonly ICartographerStore _store;
    private readonly MapRenderer _renderer;
    private readonly TerrainGrid _grid;
    private readonly ScreenshotSampler _sampler;
    private readonly TimeSpan _ttl;
    private readonly ILogger<RenderBatchProcessor> _logger;

    public RenderBatchProcessor(
        ICartographerStore store,
        MapRenderer renderer,
        IOptions<GridOptions> gridOptions,
        IOptions<RenderOptions> renderOptions,
        ILogger<RenderBatchProcessor> logger)
    {
        _store = store;
        _renderer = renderer;
        var go = gridOptions.Value;
        _grid = new TerrainGrid(go.CellSizeMeters, go.DatasetVersion);
        _sampler = new ScreenshotSampler(_grid, renderOptions.Value.PixelsPerCell);
        _ttl = TimeSpan.FromDays(go.TtlDays);
        _logger = logger;
    }

    public TerrainGrid Grid => _grid;

    public async Task ProcessJobAsync(RenderJob job, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(job.GridId, _grid.GridId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Job grid_id {job.GridId} does not match worker grid {_grid.GridId}.");
        }

        var origin = new CellIndex(job.OriginX, job.OriginY);
        var sw = _grid.CellToBounds(origin);
        var ne = _grid.CellToBounds(new CellIndex(job.OriginX + job.Width - 1, job.OriginY + job.Height - 1));
        var bbox = new LatLngBounds(sw.MinLng, sw.MinLat, ne.MaxLng, ne.MaxLat);

        var (w, h) = _sampler.ImageSizeForCells(job.Width, job.Height);
        _logger.LogInformation(
            "Rendering batch {BatchKey} {Width}x{Height} cells -> {PxW}x{PxH}px",
            job.BatchKey, job.Width, job.Height, w, h);

        var png = await _renderer.CapturePngAsync(bbox, w, h, cancellationToken).ConfigureAwait(false);
        var (rgb, imgW, imgH) = PngRgbDecoder.Decode(png);
        var samples = _sampler.ClassifyGrid(rgb, imgW, imgH, job.OriginX, job.OriginY, job.Width, job.Height);

        var now = DateTimeOffset.UtcNow;
        var records = samples.Select(s => new CellRecord(
            job.GridId,
            s.Index,
            s.Sample.Class,
            s.Sample.SecondaryClass,
            s.Sample.Confidence,
            now,
            Ttl.ComputeExpiry(now, _ttl))).ToList();

        await _store.UpsertCellsAsync(records, cancellationToken).ConfigureAwait(false);
        await _store.CompleteJobAsync(job.Id, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Upserted {Count} cells for batch {BatchKey}", records.Count, job.BatchKey);
    }

    /// <summary>
    /// Classify-only path used by golden tests (no Playwright / DB).
    /// </summary>
    public static IReadOnlyList<(CellIndex Index, CellSample Sample)> ClassifyPng(
        byte[] pngBytes,
        TerrainGrid grid,
        long originX,
        long originY,
        int cellsWide,
        int cellsHigh,
        int pixelsPerCell = 8)
    {
        var (rgb, w, h) = PngRgbDecoder.Decode(pngBytes);
        var sampler = new ScreenshotSampler(grid, pixelsPerCell);
        return sampler.ClassifyGrid(rgb, w, h, originX, originY, cellsWide, cellsHigh);
    }
}
