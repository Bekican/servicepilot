namespace ServicePilot.Domain.Auditing;

public sealed class AuditLog
{
    public const int MaxActionLength = 100;
    public const int MaxEntityTypeLength = 100;
    public const int MaxMetadataLength = 2000;

    private AuditLog()
    {
    }

    public AuditLog(
        Guid id,
        Guid organizationId,
        Guid? actorUserId,
        string action,
        string entityType,
        Guid entityId,
        DateTimeOffset occurredAtUtc,
        string? metadata = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Audit log identifier cannot be empty",
                nameof(id));
        }

        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Organization identifier cannot be empty",
                nameof(organizationId));
        }

        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Actor identifier cannot be empty",
                nameof(actorUserId));
        }

        if (entityId == Guid.Empty)
        {
            throw new ArgumentException(
                "Entity identifier cannot be empty",
                nameof(entityId));
        }

        ValidateText(
            action,
            MaxActionLength,
            nameof(action));
        ValidateText(
            entityType,
            MaxEntityTypeLength,
            nameof(entityType));

        if (metadata?.Length > MaxMetadataLength)
        {
            throw new ArgumentException(
                "Audit metadata is too long",
                nameof(metadata));
        }

        Id = id;
        OrganizationId = organizationId;
        ActorUserId = actorUserId;
        Action = action.Trim();
        EntityType = entityType.Trim();
        EntityId = entityId;
        OccurredAtUtc = occurredAtUtc;
        Metadata = metadata;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string? Metadata { get; private set; }
    public DateTimeOffset? AnonymizedAtUtc { get; private set; }

    public void Anonymize(DateTimeOffset anonymizedAtUtc)
    {
        ActorUserId = null;
        Metadata = null;
        AnonymizedAtUtc = anonymizedAtUtc;
    }

    private static void ValidateText(
        string value,
        int maxLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Audit value cannot be empty",
                parameterName);
        }

        if (value.Trim().Length > maxLength)
        {
            throw new ArgumentException(
                "Audit value is too long",
                parameterName);
        }
    }
}