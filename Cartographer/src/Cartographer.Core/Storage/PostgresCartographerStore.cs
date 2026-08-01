using Cartographer.Core.Grid;
using Npgsql;
using NpgsqlTypes;

namespace Cartographer.Core.Storage;

/// <summary>
/// PostgreSQL-backed cell store and FOR UPDATE SKIP LOCKED job queue.
/// </summary>
public sealed class PostgresCartographerStore : ICartographerStore
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresCartographerStore(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public PostgresCartographerStore(string connectionString)
        : this(NpgsqlDataSource.Create(connectionString))
    {
    }

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        var sql = await ReadEmbeddedSchemaAsync().ConfigureAwait(false);
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpsertCellsAsync(IEnumerable<CellRecord> cells, CancellationToken cancellationToken = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            insert into cells (grid_id, cell_x, cell_y, class, secondary_class, confidence, sampled_at, expires_at)
            values (@grid_id, @cell_x, @cell_y, @class, @secondary_class, @confidence, @sampled_at, @expires_at)
            on conflict (grid_id, cell_x, cell_y) do update set
              class = excluded.class,
              secondary_class = excluded.secondary_class,
              confidence = excluded.confidence,
              sampled_at = excluded.sampled_at,
              expires_at = excluded.expires_at
            """;

        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.Add(new NpgsqlParameter("grid_id", NpgsqlDbType.Text));
        cmd.Parameters.Add(new NpgsqlParameter("cell_x", NpgsqlDbType.Bigint));
        cmd.Parameters.Add(new NpgsqlParameter("cell_y", NpgsqlDbType.Bigint));
        cmd.Parameters.Add(new NpgsqlParameter("class", NpgsqlDbType.Text));
        cmd.Parameters.Add(new NpgsqlParameter("secondary_class", NpgsqlDbType.Text) { IsNullable = true });
        cmd.Parameters.Add(new NpgsqlParameter("confidence", NpgsqlDbType.Real));
        cmd.Parameters.Add(new NpgsqlParameter("sampled_at", NpgsqlDbType.TimestampTz));
        cmd.Parameters.Add(new NpgsqlParameter("expires_at", NpgsqlDbType.TimestampTz));

        foreach (var cell in cells)
        {
            cmd.Parameters["grid_id"].Value = cell.GridId;
            cmd.Parameters["cell_x"].Value = cell.Index.X;
            cmd.Parameters["cell_y"].Value = cell.Index.Y;
            cmd.Parameters["class"].Value = TerrainClassCodec.ToWire(cell.Class);
            cmd.Parameters["secondary_class"].Value = cell.SecondaryClass is { } s
                ? TerrainClassCodec.ToWire(s)
                : DBNull.Value;
            cmd.Parameters["confidence"].Value = cell.Confidence;
            cmd.Parameters["sampled_at"].Value = cell.SampledAt.UtcDateTime;
            cmd.Parameters["expires_at"].Value = cell.ExpiresAt.UtcDateTime;
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CellRecord>> GetCellsAsync(
        string gridId,
        long minX,
        long minY,
        long maxX,
        long maxY,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select grid_id, cell_x, cell_y, class, secondary_class, confidence, sampled_at, expires_at
            from cells
            where grid_id = @grid_id
              and cell_x >= @min_x and cell_x <= @max_x
              and cell_y >= @min_y and cell_y <= @max_y
            """;

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("grid_id", gridId);
        cmd.Parameters.AddWithValue("min_x", minX);
        cmd.Parameters.AddWithValue("min_y", minY);
        cmd.Parameters.AddWithValue("max_x", maxX);
        cmd.Parameters.AddWithValue("max_y", maxY);

        var list = new List<CellRecord>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(ReadCell(reader));
        }

        return list;
    }

    public async Task<bool> TryEnqueueJobAsync(
        string batchKey,
        string gridId,
        long originX,
        long originY,
        int width,
        int height,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into render_jobs (batch_key, grid_id, origin_x, origin_y, width, height, status)
            values (@batch_key, @grid_id, @origin_x, @origin_y, @width, @height, 'pending')
            on conflict (batch_key) do nothing
            """;

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("batch_key", batchKey);
        cmd.Parameters.AddWithValue("grid_id", gridId);
        cmd.Parameters.AddWithValue("origin_x", originX);
        cmd.Parameters.AddWithValue("origin_y", originY);
        cmd.Parameters.AddWithValue("width", width);
        cmd.Parameters.AddWithValue("height", height);
        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return rows > 0;
    }

    public async Task<RenderJob?> ClaimNextJobAsync(string workerId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            with next as (
              select id
              from render_jobs
              where status in ('pending', 'failed')
              order by created_at
              for update skip locked
              limit 1
            )
            update render_jobs j
            set status = 'running',
                attempts = j.attempts + 1,
                locked_at = now(),
                locked_by = @worker_id,
                updated_at = now()
            from next
            where j.id = next.id
            returning j.id, j.batch_key, j.grid_id, j.origin_x, j.origin_y, j.width, j.height, j.status, j.attempts
            """;

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("worker_id", workerId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            await reader.DisposeAsync().ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }

        var job = new RenderJob(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt64(3),
            reader.GetInt64(4),
            reader.GetInt32(5),
            reader.GetInt32(6),
            reader.GetString(7),
            reader.GetInt32(8));
        await reader.DisposeAsync().ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return job;
    }

    public async Task CompleteJobAsync(long jobId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update render_jobs
            set status = 'done', updated_at = now(), locked_at = null, locked_by = null, last_error = null
            where id = @id
            """;
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", jobId);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task FailJobAsync(long jobId, string error, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update render_jobs
            set status = 'failed', updated_at = now(), locked_at = null, locked_by = null, last_error = @error
            where id = @id
            """;
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", jobId);
        cmd.Parameters.AddWithValue("error", error);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CountPendingJobsAsync(string gridId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select count(*)::int from render_jobs
            where grid_id = @grid_id and status in ('pending', 'running', 'failed')
            """;
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("grid_id", gridId);
        var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(result);
    }

    public async Task<IReadOnlyList<CellIndex>> GetExpiredCellsAsync(
        string gridId,
        DateTimeOffset asOf,
        int limit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select cell_x, cell_y
            from cells
            where grid_id = @grid_id and expires_at <= @as_of
            order by expires_at
            limit @limit
            """;
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("grid_id", gridId);
        cmd.Parameters.AddWithValue("as_of", asOf.UtcDateTime);
        cmd.Parameters.AddWithValue("limit", limit);
        var list = new List<CellIndex>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(new CellIndex(reader.GetInt64(0), reader.GetInt64(1)));
        }

        return list;
    }

    private static CellRecord ReadCell(NpgsqlDataReader reader)
    {
        TerrainClass? secondary = reader.IsDBNull(4)
            ? null
            : TerrainClassCodec.Parse(reader.GetString(4));
        return new CellRecord(
            reader.GetString(0),
            new CellIndex(reader.GetInt64(1), reader.GetInt64(2)),
            TerrainClassCodec.Parse(reader.GetString(3)),
            secondary,
            reader.GetFloat(5),
            new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Utc)));
    }

    private static async Task<string> ReadEmbeddedSchemaAsync()
    {
        // Prefer reading from known path relative to content root via file system when present.
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "db", "schema.sql")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "db", "schema.sql")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "db", "schema.sql")),
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return await File.ReadAllTextAsync(path).ConfigureAwait(false);
            }
        }

        // Fallback inline (kept in sync with db/schema.sql)
        return """
            create table if not exists cells (
              grid_id         text        not null,
              cell_x          bigint      not null,
              cell_y          bigint      not null,
              class           text        not null,
              secondary_class text        null,
              confidence      real        not null,
              sampled_at      timestamptz not null,
              expires_at      timestamptz not null,
              primary key (grid_id, cell_x, cell_y)
            );
            create index if not exists cells_expiry on cells (grid_id, expires_at);
            create table if not exists render_jobs (
              id              bigserial   primary key,
              batch_key       text        not null unique,
              grid_id         text        not null,
              origin_x        bigint      not null,
              origin_y        bigint      not null,
              width           int         not null,
              height          int         not null,
              status          text        not null default 'pending',
              attempts        int         not null default 0,
              last_error      text        null,
              created_at      timestamptz not null default now(),
              updated_at      timestamptz not null default now(),
              locked_at       timestamptz null,
              locked_by       text        null
            );
            create index if not exists render_jobs_poll
              on render_jobs (status, created_at)
              where status in ('pending', 'failed');
            """;
    }
}
