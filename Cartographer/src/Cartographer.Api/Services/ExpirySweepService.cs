using Cartographer.Core.Grid;
using Cartographer.Core.Storage;
using Microsoft.Extensions.Options;

namespace Cartographer.Api.Services;

public sealed class ExpirySweepOptions
{
    public const string SectionName = "ExpirySweep";

    public bool Enabled { get; set; } = true;

    public int IntervalSeconds { get; set; } = 60;

    public int BatchLimit { get; set; } = 2_048;

    public int MaxBatchEdge { get; set; } = 64;
}

/// <summary>
/// Periodically finds expired cells and enqueues refresh jobs (deduped by batch key).
/// </summary>
public sealed class ExpirySweepService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ExpirySweepOptions _options;
    private readonly DiscoveryOptions _discovery;
    private readonly ILogger<ExpirySweepService> _logger;

    public ExpirySweepService(
        IServiceProvider services,
        IOptions<ExpirySweepOptions> options,
        IOptions<DiscoveryOptions> discovery,
        ILogger<ExpirySweepService> logger)
    {
        _services = services;
        _options = options.Value;
        _discovery = discovery.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Expiry sweep disabled");
            return;
        }

        var grid = new TerrainGrid(_discovery.CellSizeMeters, _discovery.DatasetVersion);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<ICartographerStore>();
                var expired = await store.GetExpiredCellsAsync(
                    grid.GridId, DateTimeOffset.UtcNow, _options.BatchLimit, stoppingToken).ConfigureAwait(false);

                if (expired.Count > 0)
                {
                    var enqueued = await EnqueueRefreshBatchesAsync(store, grid.GridId, expired, stoppingToken)
                        .ConfigureAwait(false);
                    _logger.LogInformation(
                        "Expiry sweep: {Expired} expired cells, enqueued {Enqueued} refresh batches",
                        expired.Count, enqueued);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Expiry sweep failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(5, _options.IntervalSeconds)), stoppingToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<int> EnqueueRefreshBatchesAsync(
        ICartographerStore store,
        string gridId,
        IReadOnlyList<CellIndex> expired,
        CancellationToken cancellationToken)
    {
        var minX = expired.Min(c => c.X);
        var maxX = expired.Max(c => c.X);
        var minY = expired.Min(c => c.Y);
        var maxY = expired.Max(c => c.Y);
        var width = checked((int)(maxX - minX + 1));
        var height = checked((int)(maxY - minY + 1));
        var edge = Math.Max(1, _options.MaxBatchEdge);
        var set = expired.ToHashSet();
        var enqueued = 0;

        for (var oy = 0; oy < height; oy += edge)
        {
            for (var ox = 0; ox < width; ox += edge)
            {
                var bw = Math.Min(edge, width - ox);
                var bh = Math.Min(edge, height - oy);
                var originX = minX + ox;
                var originY = minY + oy;
                var hits = set.Any(t =>
                    t.X >= originX && t.X < originX + bw &&
                    t.Y >= originY && t.Y < originY + bh);
                if (!hits)
                {
                    continue;
                }

                var key = $"{gridId}:{originX}:{originY}:{bw}:{bh}";
                if (await store.TryEnqueueJobAsync(key, gridId, originX, originY, bw, bh, cancellationToken)
                    .ConfigureAwait(false))
                {
                    enqueued++;
                }
            }
        }

        return enqueued;
    }
}
