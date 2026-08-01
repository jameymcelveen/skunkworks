using Cartographer.Core.Grid;

namespace Cartographer.Core.Storage;

/// <summary>Render batch queued for the worker.</summary>
public sealed record RenderJob(
    long Id,
    string BatchKey,
    string GridId,
    long OriginX,
    long OriginY,
    int Width,
    int Height,
    string Status,
    int Attempts);

/// <summary>Cell and job persistence.</summary>
public interface ICartographerStore
{
    Task EnsureSchemaAsync(CancellationToken cancellationToken = default);

    Task UpsertCellsAsync(IEnumerable<CellRecord> cells, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CellRecord>> GetCellsAsync(
        string gridId,
        long minX,
        long minY,
        long maxX,
        long maxY,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a pending job if the batch key is new. Returns true when inserted.
    /// </summary>
    Task<bool> TryEnqueueJobAsync(
        string batchKey,
        string gridId,
        long originX,
        long originY,
        int width,
        int height,
        CancellationToken cancellationToken = default);

    Task<RenderJob?> ClaimNextJobAsync(string workerId, CancellationToken cancellationToken = default);

    Task CompleteJobAsync(long jobId, CancellationToken cancellationToken = default);

    Task FailJobAsync(long jobId, string error, CancellationToken cancellationToken = default);

    Task<int> CountPendingJobsAsync(string gridId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns up to <paramref name="limit"/> expired cell indices for a grid.
    /// </summary>
    Task<IReadOnlyList<CellIndex>> GetExpiredCellsAsync(
        string gridId,
        DateTimeOffset asOf,
        int limit,
        CancellationToken cancellationToken = default);
}
