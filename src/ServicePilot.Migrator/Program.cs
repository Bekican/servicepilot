using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Trace;

using ServicePilot.Infrastructure;
using ServicePilot.Infrastructure.Persistence;
using ServicePilot.Observability;

HostApplicationBuilder builder =
    Host.CreateApplicationBuilder(args);

builder.AddServicePilotObservability(
    "ServicePilot.Migrator");

builder.Services.AddMigrationInfrastructure(
    builder.Configuration);

using IHost host = builder.Build();
_ = host.Services.GetRequiredService<TracerProvider>();
await using AsyncServiceScope scope =
    host.Services.CreateAsyncScope();

ILogger logger = scope.ServiceProvider
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("ServicePilot.Migrator");
EventId migrationFailedEvent =
    new(3001, "DatabaseMigrationFailed");

try
{
    using System.Diagnostics.Activity? activity =
        ServicePilotTelemetry.StartActivity(
            "database.migrate");

    ServicePilotDbContext dbContext =
        scope.ServiceProvider.GetRequiredService<
            ServicePilotDbContext>();
    IReadOnlyList<string> pendingMigrations =
        (await dbContext.Database
            .GetPendingMigrationsAsync())
        .ToArray();

    if (pendingMigrations.Count == 0)
    {
        activity?.SetTag(
            "servicepilot.migration.count",
            0);
        logger.LogInformation(
            "Database schema is already up to date.");
        return 0;
    }

    logger.LogInformation(
        "Applying {MigrationCount} database migration(s).",
        pendingMigrations.Count);
    activity?.SetTag(
        "servicepilot.migration.count",
        pendingMigrations.Count);

    await dbContext.Database.MigrateAsync();

    logger.LogInformation(
        "Database migration completed successfully.");
    return 0;
}
catch (Exception exception)
{
    logger.LogCritical(
        migrationFailedEvent,
        "Database migration failed with {ExceptionType}.",
        exception.GetType().Name);
    return 1;
}
