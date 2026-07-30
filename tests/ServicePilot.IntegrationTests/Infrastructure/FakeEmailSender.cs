using System.Collections.Concurrent;

using ServicePilot.Application.Abstractions.Email;

namespace ServicePilot.IntegrationTests.Infrastructure;

public sealed class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _messages =
        new();

    public IReadOnlyCollection<EmailMessage> Messages =>
        _messages.ToArray();

    public Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    public void Clear()
    {
        _messages.Clear();
    }
}