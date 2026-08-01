using DeerStand.Infrastructure.Data;
using DeerStand.Infrastructure.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DeerStand.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDeerStandPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<TenantConnectionInterceptor>();

        if (environment.IsEnvironment("Testing"))
        {
            var dbName = $"DeerStandTests-{Guid.NewGuid():N}";
            services.AddDbContext<DeerStandDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
            return services;
        }

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' is required outside Testing.");

        services.AddDbContext<DeerStandDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(sp.GetRequiredService<TenantConnectionInterceptor>());
        });

        return services;
    }

    public static async Task MigrateDeerStandDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (environment.IsEnvironment("Testing"))
            return;

        var db = scope.ServiceProvider.GetRequiredService<DeerStandDbContext>();
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
