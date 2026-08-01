using Cartographer.Core.Storage;
using Cartographer.Worker;
using Cartographer.Worker.Rendering;
using Npgsql;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection(WorkerOptions.SectionName));
builder.Services.Configure<GridOptions>(builder.Configuration.GetSection(GridOptions.SectionName));
builder.Services.Configure<RenderOptions>(builder.Configuration.GetSection(RenderOptions.SectionName));

var workerOpts = builder.Configuration.GetSection(WorkerOptions.SectionName).Get<WorkerOptions>() ?? new WorkerOptions();
builder.Services.AddSingleton(NpgsqlDataSource.Create(workerOpts.ConnectionString));
builder.Services.AddSingleton<ICartographerStore, PostgresCartographerStore>();
builder.Services.AddSingleton<MapRenderer>();
builder.Services.AddSingleton<RenderBatchProcessor>();
builder.Services.AddHostedService<RenderWorker>();

var host = builder.Build();
host.Run();
