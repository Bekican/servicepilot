using Microsoft.EntityFrameworkCore;
using ServicePilot.Application.Abstractions.Email;
using ServicePilot.Domain.Email;
using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.Infrastructure.Email;

internal sealed class EmailOutboxRepository(ServicePilotDbContext dbContext)
    : IEmailOutbox
{
    public void Add(EmailOutboxMessage message) => dbContext.EmailOutbox.Add(message);

    public async Task<IReadOnlyList<EmailOutboxMessage>> ClaimDueAsync(
        DateTimeOffset nowUtc,
        DateTimeOffset staleBeforeUtc,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        int boundedBatchSize = Math.Clamp(batchSize, 1, 100);
        return await dbContext.EmailOutbox.FromSqlInterpolated($"""
            WITH due AS (
                SELECT id
                FROM email_outbox
                WHERE (status = 'Pending' AND next_attempt_at_utc <= {nowUtc})
                   OR (status = 'Processing' AND processing_started_at_utc <= {staleBeforeUtc})
                ORDER BY next_attempt_at_utc NULLS FIRST, id
                FOR UPDATE SKIP LOCKED
                LIMIT {boundedBatchSize}
            )
            UPDATE email_outbox AS message
            SET status = 'Processing',
                attempt_count = message.attempt_count + 1,
                processing_started_at_utc = {nowUtc},
                next_attempt_at_utc = NULL,
                updated_at_utc = {nowUtc}
            FROM due
            WHERE message.id = due.id
            RETURNING message.*
            """).AsTracking().ToArrayAsync(cancellationToken);
    }
}
