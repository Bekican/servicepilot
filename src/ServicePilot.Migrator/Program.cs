using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ServicePilot.Infrastructure;
using ServicePilot.Infrastructure.Persistence;

HostApplicationBuilder builder =
    Host.CreateApplicationBuilder(args);

builder.Services.AddMigrationInfrastructure(
    builder.Configuration);

using IHost host = builder.Build();
await using AsyncServiceScope scope =
    host.Services.CreateAsyncScope();

ILogger logger = scope.ServiceProvider
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("ServicePilot.Migrator");

try
{
    ServicePilotDbContext dbContext =
        scope.ServiceProvider.GetRequiredService<
            ServicePilotDbContext>();
    IReadOnlyList<string> pendingMigrations =
        (await dbContext.Database
            .GetPendingMigrationsAsync())
        .ToArray();

    if (pendingMigrations.Count == 0)
    {
        logger.LogInformation(
            "Database schema is already up to date.");
        return 0;
    }

    logger.LogInformation(
        "Applying {MigrationCount} database migration(s).",
        pendingMigrations.Count);

    await dbContext.Database.MigrateAsync();

    logger.LogInformation(
        "Database migration completed successfully.");
    return 0;
}
catch (Exception exception)
{
    logger.LogCritical(
        exception,
        "Database migration failed.");
    return 1;
}
