using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

namespace ServicePilot.IntegrationTests.Infrastructure;

public sealed class TestLogSink
{
    private readonly ConcurrentQueue<string> _entries = [];

    public IReadOnlyCollection<string> Entries =>
        _entries.ToArray();

    public void Clear()
    {
        while (_entries.TryDequeue(out _))
        {
        }
    }

    public ILoggerProvider CreateProvider() =>
        new SinkLoggerProvider(_entries);

    private sealed class SinkLoggerProvider(
        ConcurrentQueue<string> entries)
        : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) =>
            new SinkLogger(entries, categoryName);

        public void Dispose()
        {
        }
    }

    private sealed class SinkLogger(
        ConcurrentQueue<string> entries,
        string categoryName)
        : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            entries.Enqueue(
                string.Join(
                    '|',
                    categoryName,
                    logLevel,
                    eventId.Name,
                    formatter(state, exception),
                    exception?.ToString() ?? string.Empty));
        }
    }
}
