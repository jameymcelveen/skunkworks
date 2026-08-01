using DeerStand.Infrastructure.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DeerStand.Infrastructure.Data;

/// <summary>Design-time factory for <c>dotnet ef migrations</c>.</summary>
public sealed class DeerStandDbContextFactory : IDesignTimeDbContextFactory<DeerStandDbContext>
{
    public DeerStandDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../DeerStand.Api"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5433;Database=deerstand;Username=deerstand;Password=deerstand_dev";

        var options = new DbContextOptionsBuilder<DeerStandDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new DeerStandDbContext(options, new TenantContext());
    }
}
