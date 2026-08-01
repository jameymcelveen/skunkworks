using System.Collections.Concurrent;
using Cartographer.Core.Grid;

namespace Cartographer.Core.Storage;

/// <summary>
/// In-memory store for tests and local dry-runs.
/// </summary>
public sealed class InMemoryCartographerStore : ICartographerStore
{
    private readonly ConcurrentDictionary<(string GridId, long X, long Y), CellRecord> _cells = new();
    private readonly ConcurrentDictionary<string, RenderJob> _jobsByKey = new();
    private long _nextJobId = 1;
    private readonly object _jobGate = new();

    public Task EnsureSchemaAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task UpsertCellsAsync(IEnumerable<CellRecord> cells, CancellationToken cancellationToken = default)
    {
        foreach (var cell in cells)
        {
            _cells[(cell.GridId, cell.Index.X, cell.Index.Y)] = cell;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CellRecord>> GetCellsAsync(
        string gridId,
        long minX,
        long minY,
        long maxX,
        long maxY,
        CancellationToken cancellationToken = default)
    {
        var list = _cells.Values
            .Where(c => c.GridId == gridId
                        && c.Index.X >= minX && c.Index.X <= maxX
                        && c.Index.Y >= minY && c.Index.Y <= maxY)
            .ToList();
        return Task.FromResult<IReadOnlyList<CellRecord>>(list);
    }

    public Task<bool> TryEnqueueJobAsync(
        string batchKey,
        string gridId,
        long originX,
        long originY,
        int width,
        int height,
        CancellationToken cancellationToken = default)
    {
        lock (_jobGate)
        {
            if (_jobsByKey.ContainsKey(batchKey))
            {
                return Task.FromResult(false);
            }

            var job = new RenderJob(
                Interlocked.Increment(ref _nextJobId),
                batchKey,
                gridId,
                originX,
                originY,
                width,
                height,
                "pending",
                0);
            _jobsByKey[batchKey] = job;
            return Task.FromResult(true);
        }
    }

    public Task<RenderJob?> ClaimNextJobAsync(string workerId, CancellationToken cancellationToken = default)
    {
        lock (_jobGate)
        {
            var next = _jobsByKey.Values
                .Where(j => j.Status is "pending" or "failed")
                .OrderBy(j => j.Id)
                .FirstOrDefault();
            if (next is null)
            {
                return Task.FromResult<RenderJob?>(null);
            }

            var claimed = next with { Status = "running", Attempts = next.Attempts + 1 };
            _jobsByKey[claimed.BatchKey] = claimed;
            return Task.FromResult<RenderJob?>(claimed);
        }
    }

    public Task CompleteJobAsync(long jobId, CancellationToken cancellationToken = default)
    {
        lock (_jobGate)
        {
            var existing = _jobsByKey.Values.FirstOrDefault(j => j.Id == jobId);
            if (existing is not null)
            {
                _jobsByKey[existing.BatchKey] = existing with { Status = "done" };
            }
        }

        return Task.CompletedTask;
    }

    public Task FailJobAsync(long jobId, string error, CancellationToken cancellationToken = default)
    {
        lock (_jobGate)
        {
            var existing = _jobsByKey.Values.FirstOrDefault(j => j.Id == jobId);
            if (existing is not null)
            {
                _jobsByKey[existing.BatchKey] = existing with { Status = "failed" };
            }
        }

        return Task.CompletedTask;
    }

    public Task<int> CountPendingJobsAsync(string gridId, CancellationToken cancellationToken = default)
    {
        var count = _jobsByKey.Values.Count(j =>
            j.GridId == gridId && j.Status is "pending" or "running" or "failed");
        return Task.FromResult(count);
    }

    public Task<IReadOnlyList<CellIndex>> GetExpiredCellsAsync(
        string gridId,
        DateTimeOffset asOf,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var list = _cells.Values
            .Where(c => c.GridId == gridId && c.ExpiresAt <= asOf)
            .OrderBy(c => c.ExpiresAt)
            .Take(limit)
            .Select(c => c.Index)
            .ToList();
        return Task.FromResult<IReadOnlyList<CellIndex>>(list);
    }

    public IReadOnlyCollection<RenderJob> SnapshotJobs() => _jobsByKey.Values.ToList();

    public void Clear()
    {
        _cells.Clear();
        lock (_jobGate)
        {
            _jobsByKey.Clear();
        }
    }
}
