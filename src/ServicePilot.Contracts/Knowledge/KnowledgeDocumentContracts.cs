namespace ServicePilot.Contracts.Knowledge;

public sealed record KnowledgeDocumentResponse(
    Guid Id,
    string OriginalFileName,
    long SizeBytes,
    string DocumentType,
    string AccessScope,
    string Status,
    int ProcessingAttemptCount,
    string? LastErrorCode,
    string? LastErrorMessage,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
