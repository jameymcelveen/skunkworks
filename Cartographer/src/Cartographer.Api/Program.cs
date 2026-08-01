using System.Globalization;
using System.Text.Json.Serialization;
using Cartographer.Api.Services;
using Cartographer.Core.Grid;
using Cartographer.Core.Storage;
using Microsoft.Extensions.Options;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DiscoveryOptions>(builder.Configuration.GetSection(DiscoveryOptions.SectionName));
builder.Services.Configure<ExpirySweepOptions>(builder.Configuration.GetSection(ExpirySweepOptions.SectionName));
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

var connectionString = builder.Configuration.GetConnectionString("Cartographer")
    ?? builder.Configuration["Worker:ConnectionString"]
    ?? "Host=localhost;Port=5432;Database=cartographer;Username=cartographer;Password=cartographer";

var useMemory = builder.Configuration.GetValue("UseInMemoryStore", false);
if (useMemory)
{
    builder.Services.AddSingleton<ICartographerStore, InMemoryCartographerStore>();
}
else
{
    builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
    builder.Services.AddSingleton<ICartographerStore, PostgresCartographerStore>();
}

builder.Services.AddSingleton<DiscoveryService>();
builder.Services.AddHostedService<ExpirySweepService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var store = scope.ServiceProvider.GetRequiredService<ICartographerStore>();
    await store.EnsureSchemaAsync();
}

app.MapGet("/health", (DiscoveryService discovery) => Results.Json(new
{
    status = "ok",
    gridId = discovery.Grid.GridId,
    attribution = "Map data © OpenStreetMap contributors (ODbL). Derived terrain cells by Cartographer."
}));

app.MapGet("/grids/{gridId}/cells", async (
    string gridId,
    string? bbox,
    DiscoveryService discovery,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(bbox))
    {
        return Results.BadRequest(new { error = "Query param bbox=minLng,minLat,maxLng,maxLat is required." });
    }

    if (!TryParseBbox(bbox, out var bounds, out var parseError))
    {
        return Results.BadRequest(new { error = parseError });
    }

    var result = await discovery.DiscoverAsync(gridId, bounds, ct);
    if (result.StatusCode == 400)
    {
        return Results.BadRequest(new { error = result.Error });
    }

    if (result.StatusCode == 404)
    {
        return Results.NotFound(new { error = result.Error });
    }

    var g = result.Grid!;
    return Results.Json(new
    {
        gridId = g.GridId,
        originX = g.OriginX,
        originY = g.OriginY,
        width = g.Width,
        height = g.Height,
        classes = g.Classes,
        pending = g.Pending,
        stale = g.Stale,
        enqueued = result.Enqueued,
        rateLimited = result.RateLimited,
        attribution = "© OpenStreetMap contributors (ODbL)"
    });
});

app.Run();

static bool TryParseBbox(string raw, out LatLngBounds bounds, out string? error)
{
    bounds = default;
    error = null;
    var parts = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length != 4)
    {
        error = "bbox must be minLng,minLat,maxLng,maxLat";
        return false;
    }

    if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var minLng)
        || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var minLat)
        || !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var maxLng)
        || !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var maxLat))
    {
        error = "bbox values must be floating-point numbers";
        return false;
    }

    bounds = new LatLngBounds(minLng, minLat, maxLng, maxLat);
    return true;
}

public partial class Program;
