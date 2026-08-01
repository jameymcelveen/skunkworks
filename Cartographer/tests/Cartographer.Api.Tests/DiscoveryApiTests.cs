using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cartographer.Core;
using Cartographer.Core.Grid;
using Cartographer.Core.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Cartographer.Api.Tests;

public sealed class DiscoveryApiTests : IClassFixture<DiscoveryApiFactory>
{
    private readonly DiscoveryApiFactory _factory;

    public DiscoveryApiTests(DiscoveryApiFactory factory) => _factory = factory;

    [Fact]
    public async Task ColdBbox_ReturnsPending_AndEnqueuesOnce()
    {
        _factory.ResetStore();
        var client = _factory.CreateClient();
        var gridId = _factory.GridId;
        var bbox = BboxForCells(0, 0, 3, 2);

        var first = await client.GetAsync($"/grids/{gridId}/cells?bbox={bbox}");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await first.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("pending").GetBoolean().ShouldBeTrue();
        body.GetProperty("stale").GetBoolean().ShouldBeFalse();
        body.GetProperty("enqueued").GetInt32().ShouldBeGreaterThan(0);
        body.GetProperty("classes").EnumerateArray().All(e => e.ValueKind == JsonValueKind.Null).ShouldBeTrue();

        var jobsAfterFirst = _factory.Store.SnapshotJobs().Count(j => j.Status == "pending");

        var second = await client.GetAsync($"/grids/{gridId}/cells?bbox={bbox}");
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body2 = await second.Content.ReadFromJsonAsync<JsonElement>();
        body2.GetProperty("enqueued").GetInt32().ShouldBe(0);
        _factory.Store.SnapshotJobs().Count(j => j.Status == "pending").ShouldBe(jobsAfterFirst);
    }

    [Fact]
    public async Task WarmBbox_ReturnsFullGrid()
    {
        _factory.ResetStore();
        var gridId = _factory.GridId;
        var now = DateTimeOffset.UtcNow;
        var cells = new List<CellRecord>();
        for (var y = 0; y < 2; y++)
        {
            for (var x = 0; x < 3; x++)
            {
                cells.Add(new CellRecord(
                    gridId,
                    new CellIndex(x, y),
                    TerrainClass.Grass,
                    null,
                    1f,
                    now,
                    now.AddDays(30)));
            }
        }

        await _factory.Store.UpsertCellsAsync(cells);

        var client = _factory.CreateClient();
        var bbox = BboxForCells(0, 0, 2, 1);
        var response = await client.GetAsync($"/grids/{gridId}/cells?bbox={bbox}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("pending").GetBoolean().ShouldBeFalse();
        body.GetProperty("stale").GetBoolean().ShouldBeFalse();
        body.GetProperty("enqueued").GetInt32().ShouldBe(0);
        body.GetProperty("classes").EnumerateArray()
            .Select(e => e.GetString())
            .ShouldAllBe(c => c == "grass");
    }

    [Fact]
    public async Task ExpiredCell_IsServedStale_AndReEnqueuedExactlyOnce()
    {
        _factory.ResetStore();
        var gridId = _factory.GridId;
        var now = DateTimeOffset.UtcNow;
        await _factory.Store.UpsertCellsAsync(
        [
            new CellRecord(
                gridId,
                new CellIndex(0, 0),
                TerrainClass.Water,
                null,
                0.9f,
                now.AddDays(-100),
                now.AddDays(-10)),
        ]);

        var client = _factory.CreateClient();
        var bbox = BboxForCells(0, 0, 0, 0);

        var first = await client.GetAsync($"/grids/{gridId}/cells?bbox={bbox}");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await first.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("stale").GetBoolean().ShouldBeTrue();
        body.GetProperty("pending").GetBoolean().ShouldBeFalse();
        body.GetProperty("classes")[0].GetString().ShouldBe("water");
        body.GetProperty("enqueued").GetInt32().ShouldBe(1);
        _factory.Store.SnapshotJobs().Count(j => j.Status == "pending").ShouldBe(1);

        var second = await client.GetAsync($"/grids/{gridId}/cells?bbox={bbox}");
        var body2 = await second.Content.ReadFromJsonAsync<JsonElement>();
        body2.GetProperty("stale").GetBoolean().ShouldBeTrue();
        body2.GetProperty("enqueued").GetInt32().ShouldBe(0);
        _factory.Store.SnapshotJobs().Count(j => j.Status == "pending").ShouldBe(1);
    }

    [Fact]
    public async Task OversizedBbox_Returns400()
    {
        _factory.ResetStore();
        var client = _factory.CreateClient();
        // Huge geographic span
        var response = await client.GetAsync($"/grids/{_factory.GridId}/cells?bbox=-180,-85,180,85");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static string BboxForCells(long minX, long minY, long maxX, long maxY)
    {
        var grid = new TerrainGrid(10, "v1");
        var sw = grid.CellToBounds(new CellIndex(minX, minY));
        var ne = grid.CellToBounds(new CellIndex(maxX, maxY));
        // Stay inside exclusive max edge
        var maxLng = ne.MaxLng - 1e-10;
        var maxLat = ne.MaxLat - 1e-10;
        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{sw.MinLng},{sw.MinLat},{maxLng},{maxLat}");
    }
}

public sealed class DiscoveryApiFactory : WebApplicationFactory<Program>
{
    public InMemoryCartographerStore Store { get; } = new();

    public string GridId { get; } = new TerrainGrid(10, "v1").GridId;

    public void ResetStore() => Store.Clear();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("UseInMemoryStore", "true");
        builder.UseSetting("ExpirySweep:Enabled", "false");
        builder.ConfigureServices(services =>
        {
            var existing = services.Where(d => d.ServiceType == typeof(ICartographerStore)).ToList();
            foreach (var d in existing)
            {
                services.Remove(d);
            }

            services.AddSingleton<ICartographerStore>(Store);
        });
    }
}
