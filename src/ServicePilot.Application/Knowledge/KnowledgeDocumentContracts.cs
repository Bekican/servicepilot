namespace ServicePilot.Application.Knowledge;

public sealed record UploadKnowledgeDocument(
    string OriginalFileName,
    string ContentType,
    Stream Content,
    long SizeBytes,
    string DocumentType,
    string? AccessScope);

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

public sealed record KnowledgeDocumentDownload(
    Stream Content,
    string ContentType,
    string OriginalFileName);
