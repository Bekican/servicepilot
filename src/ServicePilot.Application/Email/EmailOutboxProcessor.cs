using ServicePilot.Application.Abstractions.Email;
using ServicePilot.Application.Abstractions.Persistence;

namespace ServicePilot.Application.Email;

public sealed class EmailOutboxProcessor(
    IEmailOutbox outbox,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<int> ProcessDueAsync(
        int batchSize = 25,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        var messages = await outbox.ClaimDueAsync(
            nowUtc,
            nowUtc - TimeSpan.FromMinutes(5),
            batchSize,
            cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await emailSender.SendAsync(
                    new EmailMessage(message.Recipient, message.Subject, message.Body!),
                    cancellationToken);
                message.MarkSent(timeProvider.GetUtcNow());
            }
            catch (EmailDeliveryException exception)
            {
                message.MarkDeliveryFailed(exception.Message, timeProvider.GetUtcNow());
            }
        }

        if (messages.Count > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);
        return messages.Count;
    }
}
