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

public sealed record AskKnowledgeRequest(string Question);

public sealed record KnowledgeAnswerResponse(
    string Answer,
    bool InsufficientEvidence,
    IReadOnlyList<KnowledgeCitationResponse> Citations);

public sealed record KnowledgeCitationResponse(
    string SourceId,
    Guid DocumentId,
    string OriginalFileName,
    int PageNumber,
    string ContentUrl);
