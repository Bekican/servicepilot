namespace ServicePilot.Domain.Email;

public enum EmailOutboxStatus
{
    Pending,
    Processing,
    Sent,
    Failed
}

public sealed class EmailOutboxMessage
{
    public const int MaxRecipientLength = 320;
    public const int MaxSubjectLength = 200;
    public const int MaxBodyLength = 4000;
    public const int MaxErrorLength = 500;

    private EmailOutboxMessage() { }

    public EmailOutboxMessage(
        Guid id,
        string recipient,
        string subject,
        string body,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Identifier cannot be empty", nameof(id));
        Recipient = Required(recipient, MaxRecipientLength, nameof(recipient));
        Subject = Required(subject, MaxSubjectLength, nameof(subject));
        Body = Required(body, MaxBodyLength, nameof(body));
        Id = id;
        Status = EmailOutboxStatus.Pending;
        NextAttemptAtUtc = createdAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string Recipient { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string? Body { get; private set; }
    public EmailOutboxStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? NextAttemptAtUtc { get; private set; }
    public DateTimeOffset? ProcessingStartedAtUtc { get; private set; }
    public DateTimeOffset? SentAtUtc { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void MarkSent(DateTimeOffset nowUtc)
    {
        Status = EmailOutboxStatus.Sent;
        SentAtUtc = nowUtc;
        ProcessingStartedAtUtc = null;
        NextAttemptAtUtc = null;
        LastError = null;
        Body = null;
        UpdatedAtUtc = nowUtc;
    }

    public void MarkDeliveryFailed(string error, DateTimeOffset nowUtc)
    {
        LastError = string.IsNullOrWhiteSpace(error)
            ? "Email delivery failed"
            : error.Trim()[..Math.Min(error.Trim().Length, MaxErrorLength)];
        ProcessingStartedAtUtc = null;
        UpdatedAtUtc = nowUtc;
        if (AttemptCount >= 3)
        {
            Status = EmailOutboxStatus.Failed;
            NextAttemptAtUtc = null;
            Body = null;
            return;
        }

        Status = EmailOutboxStatus.Pending;
        NextAttemptAtUtc = nowUtc + (AttemptCount == 1
            ? TimeSpan.FromMinutes(1)
            : TimeSpan.FromMinutes(5));
    }

    private static string Required(string value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
            throw new ArgumentException("Email outbox value is invalid", parameterName);
        return value.Trim();
    }
}
