using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;

using Microsoft.EntityFrameworkCore.Diagnostics;

using Npgsql;

using ServicePilot.Observability;

namespace ServicePilot.Infrastructure.Persistence;

internal sealed class SafeDatabaseTracingInterceptor
    : DbCommandInterceptor
{
    private readonly ConcurrentDictionary<Guid, Activity>
        _activeActivities = new();

    public override InterceptionResult<DbDataReader>
        ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
    {
        StartActivity(eventData.CommandId);

        return base.ReaderExecuting(
            command,
            eventData,
            result);
    }

    public override ValueTask<
        InterceptionResult<DbDataReader>>
        ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
    {
        StartActivity(eventData.CommandId);

        return base.ReaderExecutingAsync(
            command,
            eventData,
            result,
            cancellationToken);
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        CompleteActivity(eventData.CommandId);

        return base.ReaderExecuted(
            command,
            eventData,
            result);
    }

    public override ValueTask<DbDataReader>
        ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
    {
        CompleteActivity(eventData.CommandId);

        return base.ReaderExecutedAsync(
            command,
            eventData,
            result,
            cancellationToken);
    }

    public override InterceptionResult<object>
        ScalarExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result)
    {
        StartActivity(eventData.CommandId);

        return base.ScalarExecuting(
            command,
            eventData,
            result);
    }

    public override ValueTask<
        InterceptionResult<object>>
        ScalarExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
    {
        StartActivity(eventData.CommandId);

        return base.ScalarExecutingAsync(
            command,
            eventData,
            result,
            cancellationToken);
    }

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        CompleteActivity(eventData.CommandId);

        return base.ScalarExecuted(
            command,
            eventData,
            result);
    }

    public override ValueTask<object?>
        ScalarExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            object? result,
            CancellationToken cancellationToken = default)
    {
        CompleteActivity(eventData.CommandId);

        return base.ScalarExecutedAsync(
            command,
            eventData,
            result,
            cancellationToken);
    }

    public override InterceptionResult<int>
        NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
    {
        StartActivity(eventData.CommandId);

        return base.NonQueryExecuting(
            command,
            eventData,
            result);
    }

    public override ValueTask<
        InterceptionResult<int>>
        NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
    {
        StartActivity(eventData.CommandId);

        return base.NonQueryExecutingAsync(
            command,
            eventData,
            result,
            cancellationToken);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        CompleteActivity(eventData.CommandId);

        return base.NonQueryExecuted(
            command,
            eventData,
            result);
    }

    public override ValueTask<int>
        NonQueryExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
    {
        CompleteActivity(eventData.CommandId);

        return base.NonQueryExecutedAsync(
            command,
            eventData,
            result,
            cancellationToken);
    }

    public override void CommandFailed(
        DbCommand command,
        CommandErrorEventData eventData)
    {
        FailActivity(
            eventData.CommandId,
            eventData.Exception);

        base.CommandFailed(command, eventData);
    }

    public override Task CommandFailedAsync(
        DbCommand command,
        CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        FailActivity(
            eventData.CommandId,
            eventData.Exception);

        return base.CommandFailedAsync(
            command,
            eventData,
            cancellationToken);
    }

    public override void CommandCanceled(
        DbCommand command,
        CommandEndEventData eventData)
    {
        CompleteActivity(eventData.CommandId);

        base.CommandCanceled(command, eventData);
    }

    public override Task CommandCanceledAsync(
        DbCommand command,
        CommandEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        CompleteActivity(eventData.CommandId);

        return base.CommandCanceledAsync(
            command,
            eventData,
            cancellationToken);
    }

    private void StartActivity(Guid commandId)
    {
        Activity? activity =
            ServicePilotTelemetry.StartDatabaseActivity();

        if (activity is null)
        {
            return;
        }

        activity.SetTag(
            "db.system.name",
            "postgresql");

        if (!_activeActivities.TryAdd(
                commandId,
                activity))
        {
            activity.Stop();
        }
    }

    private void CompleteActivity(Guid commandId)
    {
        if (_activeActivities.TryRemove(
                commandId,
                out Activity? activity))
        {
            activity.Stop();
        }
    }

    private void FailActivity(
        Guid commandId,
        Exception exception)
    {
        if (!_activeActivities.TryRemove(
                commandId,
                out Activity? activity))
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Error);

        activity.SetTag(
            "exception.type",
            exception.GetType().FullName
                ?? exception.GetType().Name);

        if (exception is PostgresException postgresException
            && IsSafePostgresStatusCode(
                postgresException.SqlState))
        {
            activity.SetTag(
                "db.response.status_code",
                postgresException.SqlState);
        }

        activity.Stop();
    }

    private static bool IsSafePostgresStatusCode(
        string? value)
    {
        if (value is null || value.Length != 5)
        {
            return false;
        }

        foreach (char character in value)
        {
            bool isNumber =
                character is >= '0' and <= '9';

            bool isUppercaseLetter =
                character is >= 'A' and <= 'Z';

            if (!isNumber && !isUppercaseLetter)
            {
                return false;
            }
        }

        return true;
    }
}
