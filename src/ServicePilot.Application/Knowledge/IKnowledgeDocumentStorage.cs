namespace ServicePilot.Application.Knowledge;

public interface IKnowledgeDocumentStorage
{
    Task<StoredKnowledgeFile> SavePdfAsync(
        Guid organizationId,
        Guid documentId,
        Stream content,
        long maximumBytes,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}

public sealed record StoredKnowledgeFile(
    string StorageKey,
    string ChecksumSha256,
    long SizeBytes);
