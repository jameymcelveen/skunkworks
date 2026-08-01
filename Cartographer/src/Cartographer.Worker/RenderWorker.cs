using Cartographer.Core.Storage;
using Cartographer.Worker.Rendering;
using Microsoft.Extensions.Options;

namespace Cartographer.Worker;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    public string WorkerId { get; set; } = Environment.MachineName;

    public int PollIntervalMs { get; set; } = 1000;

    public string ConnectionString { get; set; } = "Host=localhost;Port=5432;Database=cartographer;Username=cartographer;Password=cartographer";
}

public sealed class RenderWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly WorkerOptions _options;
    private readonly ILogger<RenderWorker> _logger;

    public RenderWorker(IServiceProvider services, IOptions<WorkerOptions> options, ILogger<RenderWorker> logger)
    {
        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ICartographerStore>();
        var processor = scope.ServiceProvider.GetRequiredService<RenderBatchProcessor>();

        await store.EnsureSchemaAsync(stoppingToken).ConfigureAwait(false);
        _logger.LogInformation("Cartographer worker {WorkerId} polling jobs for grid {GridId}",
            _options.WorkerId, processor.Grid.GridId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await store.ClaimNextJobAsync(_options.WorkerId, stoppingToken).ConfigureAwait(false);
                if (job is null)
                {
                    await Task.Delay(_options.PollIntervalMs, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                try
                {
                    await processor.ProcessJobAsync(job, stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Job {JobId} failed", job.Id);
                    await store.FailJobAsync(job.Id, ex.Message, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker loop error");
                await Task.Delay(_options.PollIntervalMs, stoppingToken).ConfigureAwait(false);
            }
        }
    }
}
