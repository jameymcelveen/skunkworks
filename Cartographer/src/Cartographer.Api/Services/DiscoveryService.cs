using Cartographer.Core;
using Cartographer.Core.Grid;
using Cartographer.Core.Storage;
using Microsoft.Extensions.Options;

namespace Cartographer.Api.Services;

public sealed class DiscoveryOptions
{
    public const string SectionName = "Discovery";

    public double CellSizeMeters { get; set; } = TerrainGrid.DefaultCellSizeMeters;

    public string DatasetVersion { get; set; } = "v1";

    /// <summary>Maximum cells in a single bbox request.</summary>
    public int MaxBboxCells { get; set; } = 10_000;

    /// <summary>Maximum pending/running/failed jobs per grid before 429.</summary>
    public int MaxPendingJobsPerGrid { get; set; } = 32;

    /// <summary>Max width/height of a single render batch in cells.</summary>
    public int MaxBatchEdge { get; set; } = 64;
}

public sealed class DiscoveryService
{
    private readonly ICartographerStore _store;
    private readonly TerrainGrid _grid;
    private readonly DiscoveryOptions _options;

    public DiscoveryService(ICartographerStore store, IOptions<DiscoveryOptions> options)
    {
        _store = store;
        _options = options.Value;
        _grid = new TerrainGrid(_options.CellSizeMeters, _options.DatasetVersion);
    }

    public TerrainGrid Grid => _grid;

    public async Task<DiscoveryResult> DiscoverAsync(string gridId, LatLngBounds bbox, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(gridId, _grid.GridId, StringComparison.OrdinalIgnoreCase))
        {
            return DiscoveryResult.NotFound($"Unknown grid_id. Expected {_grid.GridId} for cell_size={_options.CellSizeMeters} dataset={_options.DatasetVersion}.");
        }

        if (!bbox.IsValid)
        {
            return DiscoveryResult.BadRequest("Invalid bbox: expected minLng,minLat,maxLng,maxLat with min <= max.");
        }

        var cellCount = _grid.CountCellsInBbox(bbox);
        if (cellCount == 0)
        {
            return DiscoveryResult.BadRequest("Bbox covers no cells.");
        }

        if (cellCount > _options.MaxBboxCells)
        {
            return DiscoveryResult.BadRequest(
                $"Bbox covers {cellCount} cells; max allowed is {_options.MaxBboxCells}.");
        }

        var cells = _grid.BboxToCells(bbox);

        var minX = cells.Min(c => c.X);
        var maxX = cells.Max(c => c.X);
        var minY = cells.Min(c => c.Y);
        var maxY = cells.Max(c => c.Y);
        var width = checked((int)(maxX - minX + 1));
        var height = checked((int)(maxY - minY + 1));

        var knownList = await _store.GetCellsAsync(gridId, minX, minY, maxX, maxY, cancellationToken)
            .ConfigureAwait(false);
        var known = knownList.ToDictionary(c => c.Index);
        var now = DateTimeOffset.UtcNow;

        var missingOrExpired = new List<CellIndex>();
        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var index = new CellIndex(x, y);
                if (!known.TryGetValue(index, out var record) || record.ExpiresAt <= now)
                {
                    missingOrExpired.Add(index);
                }
            }
        }

        var enqueued = 0;
        if (missingOrExpired.Count > 0)
        {
            var pending = await _store.CountPendingJobsAsync(gridId, cancellationToken).ConfigureAwait(false);
            if (pending >= _options.MaxPendingJobsPerGrid)
            {
                // Still return what we have; signal rate limit via flag rather than dropping the payload.
                var limited = CompactGridBuilder.Build(gridId, minX, minY, width, height, known, now);
                return DiscoveryResult.Ok(limited with { Pending = true }, enqueued: 0, rateLimited: true);
            }

            foreach (var batch in ChunkBatches(missingOrExpired, minX, minY, width, height))
            {
                var key = $"{gridId}:{batch.OriginX}:{batch.OriginY}:{batch.Width}:{batch.Height}";
                if (await _store.TryEnqueueJobAsync(
                        key, gridId, batch.OriginX, batch.OriginY, batch.Width, batch.Height, cancellationToken)
                    .ConfigureAwait(false))
                {
                    enqueued++;
                }
            }
        }

        var grid = CompactGridBuilder.Build(gridId, minX, minY, width, height, known, now);
        return DiscoveryResult.Ok(grid, enqueued);
    }

    private IEnumerable<(long OriginX, long OriginY, int Width, int Height)> ChunkBatches(
        List<CellIndex> targets,
        long minX,
        long minY,
        int width,
        int height)
    {
        // One covering rectangle, split into MaxBatchEdge tiles.
        var edge = Math.Max(1, _options.MaxBatchEdge);
        for (var oy = 0; oy < height; oy += edge)
        {
            for (var ox = 0; ox < width; ox += edge)
            {
                var bw = Math.Min(edge, width - ox);
                var bh = Math.Min(edge, height - oy);
                var originX = minX + ox;
                var originY = minY + oy;
                var intersects = targets.Any(t =>
                    t.X >= originX && t.X < originX + bw &&
                    t.Y >= originY && t.Y < originY + bh);
                if (intersects)
                {
                    yield return (originX, originY, bw, bh);
                }
            }
        }
    }
}

public sealed record DiscoveryResult(
    int StatusCode,
    CompactCellGrid? Grid,
    string? Error,
    int Enqueued,
    bool RateLimited)
{
    public static DiscoveryResult Ok(CompactCellGrid grid, int enqueued, bool rateLimited = false)
        => new(200, grid, null, enqueued, rateLimited);

    public static DiscoveryResult BadRequest(string error) => new(400, null, error, 0, false);

    public static DiscoveryResult NotFound(string error) => new(404, null, error, 0, false);
}
