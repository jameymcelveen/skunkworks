using DeerStand.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDeerStandPersistence(builder.Configuration, builder.Environment);

var app = builder.Build();

await app.Services.MigrateDeerStandDatabaseAsync();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program;
