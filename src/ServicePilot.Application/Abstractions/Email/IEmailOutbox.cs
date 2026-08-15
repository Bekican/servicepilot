using ServicePilot.Domain.Email;

namespace ServicePilot.Application.Abstractions.Email;

public interface IEmailOutbox
{
    void Add(EmailOutboxMessage message);

    Task<IReadOnlyList<EmailOutboxMessage>> ClaimDueAsync(
        DateTimeOffset nowUtc,
        DateTimeOffset staleBeforeUtc,
        int batchSize,
        CancellationToken cancellationToken = default);
}
