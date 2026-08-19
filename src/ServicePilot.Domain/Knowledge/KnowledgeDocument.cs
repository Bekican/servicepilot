using ServicePilot.Domain.Users;

namespace ServicePilot.Domain.Knowledge;

public sealed class KnowledgeDocument
{
    public const int MaxOriginalFileNameLength = 255;
    public const int MaxContentTypeLength = 100;
    public const int MaxStorageKeyLength = 500;
    public const int ChecksumLength = 64;
    public const int MaxErrorCodeLength = 100;
    public const int MaxErrorMessageLength = 500;
    public const int MaxProcessingAttempts = 3;

    private KnowledgeDocument()
    {
    }

    public KnowledgeDocument(
        Guid id,
        Guid organizationId,
        Guid uploadedByUserId,
        string originalFileName,
        string contentType,
        string storageKey,
        string checksumSha256,
        long sizeBytes,
        KnowledgeDocumentType type,
        KnowledgeDocumentAccessScope accessScope,
        DateTimeOffset createdAtUtc)
    {
        ValidateIdentifier(id, nameof(id));
        ValidateIdentifier(organizationId, nameof(organizationId));
        ValidateIdentifier(uploadedByUserId, nameof(uploadedByUserId));

        Id = id;
        OrganizationId = organizationId;
        UploadedByUserId = uploadedByUserId;
        OriginalFileName = RequiredText(
            originalFileName,
            MaxOriginalFileNameLength,
            nameof(originalFileName));
        ContentType = RequiredText(
            contentType,
            MaxContentTypeLength,
            nameof(contentType));
        StorageKey = RequiredText(
            storageKey,
            MaxStorageKeyLength,
            nameof(storageKey));
        ChecksumSha256 = ValidateChecksum(checksumSha256);

        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sizeBytes),
                "Document size must be positive");
        }

        EnsureScopeAllowed(type, accessScope);
        SizeBytes = sizeBytes;
        Type = type;
        AccessScope = accessScope;
        Status = KnowledgeDocumentStatus.Pending;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public string ChecksumSha256 { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public KnowledgeDocumentType Type { get; private set; }
    public KnowledgeDocumentAccessScope AccessScope { get; private set; }
    public KnowledgeDocumentStatus Status { get; private set; }
    public int ProcessingAttemptCount { get; private set; }
    public DateTimeOffset? ProcessingStartedAtUtc { get; private set; }
    public string? LastErrorCode { get; private set; }
    public string? LastErrorMessage { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static KnowledgeDocumentAccessScope DefaultScopeFor(
        KnowledgeDocumentType type) =>
        type is KnowledgeDocumentType.TechnicalProcedure
            or KnowledgeDocumentType.Manual
                ? KnowledgeDocumentAccessScope.Shared
                : KnowledgeDocumentAccessScope.Operations;

    public bool CanBeAccessedByRole(string role) =>
        AccessScope switch
        {
            KnowledgeDocumentAccessScope.Shared =>
                UserRoles.IsSupported(role),
            KnowledgeDocumentAccessScope.Operations =>
                role is UserRoles.Owner
                    or UserRoles.Admin
                    or UserRoles.Dispatcher,
            KnowledgeDocumentAccessScope.Management =>
                role is UserRoles.Owner
                    or UserRoles.Admin,
            _ => false
        };

    public void MarkProcessing(DateTimeOffset startedAtUtc)
    {
        if (Status is not KnowledgeDocumentStatus.Pending
            and not KnowledgeDocumentStatus.Processing)
        {
            throw new InvalidOperationException(
                "Only pending or stale processing documents can be claimed");
        }

        if (ProcessingAttemptCount >= MaxProcessingAttempts)
        {
            throw new InvalidOperationException(
                "Document processing attempts are exhausted");
        }

        Status = KnowledgeDocumentStatus.Processing;
        ProcessingAttemptCount++;
        ProcessingStartedAtUtc = startedAtUtc;
        LastErrorCode = null;
        LastErrorMessage = null;
        UpdatedAtUtc = startedAtUtc;
    }

    public void MarkReady(DateTimeOffset completedAtUtc)
    {
        EnsureProcessing();
        Status = KnowledgeDocumentStatus.Ready;
        ProcessingStartedAtUtc = null;
        LastErrorCode = null;
        LastErrorMessage = null;
        UpdatedAtUtc = completedAtUtc;
    }

    public void MarkFailed(
        string errorCode,
        string errorMessage,
        DateTimeOffset failedAtUtc)
    {
        EnsureProcessing();
        Status = KnowledgeDocumentStatus.Failed;
        ProcessingStartedAtUtc = null;
        LastErrorCode = RequiredText(
            errorCode,
            MaxErrorCodeLength,
            nameof(errorCode));
        LastErrorMessage = RequiredText(
            errorMessage,
            MaxErrorMessageLength,
            nameof(errorMessage));
        UpdatedAtUtc = failedAtUtc;
    }

    public void Retry(DateTimeOffset requestedAtUtc)
    {
        if (Status != KnowledgeDocumentStatus.Failed)
        {
            throw new InvalidOperationException(
                "Only failed documents can be retried");
        }

        Status = KnowledgeDocumentStatus.Pending;
        ProcessingAttemptCount = 0;
        ProcessingStartedAtUtc = null;
        LastErrorCode = null;
        LastErrorMessage = null;
        UpdatedAtUtc = requestedAtUtc;
    }

    private void EnsureProcessing()
    {
        if (Status != KnowledgeDocumentStatus.Processing)
        {
            throw new InvalidOperationException(
                "Document is not being processed");
        }
    }

    private static void EnsureScopeAllowed(
        KnowledgeDocumentType type,
        KnowledgeDocumentAccessScope accessScope)
    {
        if (type == KnowledgeDocumentType.CustomerServiceReport
            && accessScope == KnowledgeDocumentAccessScope.Shared)
        {
            throw new ArgumentException(
                "Customer service reports cannot use shared access",
                nameof(accessScope));
        }
    }

    private static string ValidateChecksum(string value)
    {
        string checksum = RequiredText(
            value,
            ChecksumLength,
            nameof(value));

        if (checksum.Length != ChecksumLength
            || checksum.Any(character =>
                !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                "Checksum must be a SHA-256 hexadecimal value",
                nameof(value));
        }

        return checksum.ToLowerInvariant();
    }

    private static string RequiredText(
        string? value,
        int maxLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Required value cannot be empty",
                parameterName);
        }

        string trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException(
                "Value is too long",
                parameterName);
        }

        return trimmed;
    }

    private static void ValidateIdentifier(
        Guid identifier,
        string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException(
                "Identifier cannot be empty",
                parameterName);
        }
    }
}
