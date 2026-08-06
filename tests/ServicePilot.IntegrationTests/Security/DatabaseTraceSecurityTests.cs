using System.Collections.Concurrent;
using System.Diagnostics;

using Microsoft.EntityFrameworkCore;

using Npgsql;

using ServicePilot.IntegrationTests.Infrastructure;
using ServicePilot.Observability;

namespace ServicePilot.IntegrationTests.Security;

[Collection(IntegrationTestCollection.Name)]
public sealed class DatabaseTraceSecurityTests(
    ServicePilotApiFactory factory)
{
    [Fact]
    public async Task SuccessfulCommand_ShouldBeChildOfCurrentActivity()
    {
        ConcurrentQueue<Activity> stoppedActivities =
            new();
        AllowedTraceTagProcessor processor = new();
        using ActivityListener listener = new()
        {
            ShouldListenTo = source =>
                source.Name.Equals(
                    ServicePilotTelemetry
                        .DatabaseActivitySourceName,
                    StringComparison.Ordinal)
                || source.Name.Equals(
                    ServicePilotTelemetry.ActivitySourceName,
                    StringComparison.Ordinal),
            Sample = static (ref ActivityCreationOptions<
                    ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                processor.OnEnd(activity);
                stoppedActivities.Enqueue(activity);
            }
        };
        ActivitySource.AddActivityListener(listener);

        using Activity? parent =
            ServicePilotTelemetry.StartRootActivity(
                "test.parent");
        Assert.NotNull(parent);

        await factory.ExecuteDbContextAsync(
            async dbContext =>
                await dbContext.Database
                    .ExecuteSqlRawAsync("SELECT 1"));

        parent.Stop();

        Activity command = Assert.Single(
            stoppedActivities,
            activity => activity.DisplayName
                == "postgresql.command");
        Assert.Equal(parent.TraceId, command.TraceId);
        Assert.Equal(
            parent.SpanId,
            command.ParentSpanId);
        Assert.NotEqual(
            ActivityStatusCode.Error,
            command.Status);
        Assert.Null(command.StatusDescription);
        Assert.Empty(command.Events);
        Assert.Equal(
            "postgresql",
            command.GetTagItem("db.system.name"));
    }

    [Fact]
    public async Task FailedCommand_ShouldEmitSanitizedStableSpan()
    {
        const string secretMarker =
            "secret_customer_marker";
        ConcurrentQueue<Activity> stoppedActivities =
            new();
        AllowedTraceTagProcessor processor = new();
        using ActivityListener listener = new()
        {
            ShouldListenTo = source =>
                source.Name.Equals(
                    ServicePilotTelemetry
                        .DatabaseActivitySourceName,
                    StringComparison.Ordinal),
            Sample = static (ref ActivityCreationOptions<
                    ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                processor.OnEnd(activity);
                stoppedActivities.Enqueue(activity);
            }
        };
        ActivitySource.AddActivityListener(listener);

        await Assert.ThrowsAsync<PostgresException>(
            () => factory.ExecuteDbContextAsync(
                async dbContext =>
                    await dbContext.Database
                        .ExecuteSqlRawAsync(
                            $"SELECT * FROM {secretMarker}")));

        Activity command = Assert.Single(
            stoppedActivities,
            activity => activity.DisplayName
                == "postgresql.command");
        Assert.Equal(
            ActivityStatusCode.Error,
            command.Status);
        Assert.Equal(
            ServicePilotTelemetry
                .DatabaseActivitySourceName,
            command.Source.Name);
        Assert.Null(command.StatusDescription);
        Assert.Empty(command.Events);
        Assert.Equal(
            "42P01",
            command.GetTagItem(
                "db.response.status_code"));
        Assert.Equal(
            typeof(PostgresException).FullName,
            command.GetTagItem("exception.type"));
        Assert.All(
            command.TagObjects,
            tag => Assert.Contains(
                tag.Key,
                new HashSet<string>(
                    StringComparer.Ordinal)
                {
                    "db.system.name",
                    "db.operation.name",
                    "db.response.status_code",
                    "exception.type"
                }));

        string exportedShape = string.Join(
            '|',
            command.DisplayName,
            command.StatusDescription,
            string.Join('|', command.TagObjects),
            string.Join('|', command.Events));
        Assert.DoesNotContain(
            secretMarker,
            exportedShape,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "Password=",
            exportedShape,
            StringComparison.OrdinalIgnoreCase);
    }
}
