using Cartographer.Core;
using Cartographer.Core.Grid;
using Cartographer.Core.Storage;
using Shouldly;

namespace Cartographer.Api.Tests;

public sealed class ExpirySweepLogicTests
{
    [Fact]
    public async Task GetExpiredCells_ReturnsOnlyExpired_AndEnqueueIsIdempotent()
    {
        var store = new InMemoryCartographerStore();
        var grid = new TerrainGrid(10, "v1");
        var now = DateTimeOffset.UtcNow;

        await store.UpsertCellsAsync(
        [
            new CellRecord(grid.GridId, new CellIndex(1, 1), TerrainClass.Dirt, null, 1f, now.AddDays(-1), now.AddHours(-1)),
            new CellRecord(grid.GridId, new CellIndex(2, 2), TerrainClass.Grass, null, 1f, now, now.AddDays(30)),
        ]);

        var expired = await store.GetExpiredCellsAsync(grid.GridId, now, 100);
        expired.Count.ShouldBe(1);
        expired[0].ShouldBe(new CellIndex(1, 1));

        var key = $"{grid.GridId}:1:1:1:1";
        (await store.TryEnqueueJobAsync(key, grid.GridId, 1, 1, 1, 1)).ShouldBeTrue();
        (await store.TryEnqueueJobAsync(key, grid.GridId, 1, 1, 1, 1)).ShouldBeFalse();
        store.SnapshotJobs().Count(j => j.Status == "pending").ShouldBe(1);
    }
}
