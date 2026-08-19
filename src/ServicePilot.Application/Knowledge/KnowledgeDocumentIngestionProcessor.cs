using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Domain.Knowledge;

namespace ServicePilot.Application.Knowledge;

public sealed class KnowledgeDocumentIngestionProcessor(
    IKnowledgeDocumentRepository documentRepository,
    IKnowledgeDocumentIndexRepository indexRepository,
    IKnowledgeDocumentStorage storage,
    IPdfTextExtractor pdfTextExtractor,
    ITextEmbeddingClient embeddingClient,
    KnowledgeTextChunker chunker,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public const int MaximumPages = 300;
    public const int MaximumExtractedCharacters = 2_000_000;

    public async Task<int> ProcessPendingAsync(
        int batchSize = 2,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        IReadOnlyList<KnowledgeDocument> documents =
            await documentRepository.ClaimPendingAsync(
                nowUtc,
                nowUtc - TimeSpan.FromMinutes(10),
                batchSize,
                cancellationToken);

        foreach (KnowledgeDocument document in documents)
        {
            await ProcessOneAsync(document, cancellationToken);
        }

        return documents.Count;
    }

    private async Task ProcessOneAsync(
        KnowledgeDocument document,
        CancellationToken cancellationToken)
    {
        try
        {
            await using Stream pdf = await storage.OpenReadAsync(
                document.StorageKey,
                cancellationToken);
            IReadOnlyList<ExtractedPdfPage> pages =
                await pdfTextExtractor.ExtractAsync(
                    pdf,
                    MaximumPages,
                    MaximumExtractedCharacters,
                    cancellationToken);
            IReadOnlyList<KnowledgeTextChunk> textChunks =
                chunker.Chunk(pages);

            if (textChunks.Count == 0)
            {
                throw new KnowledgeDocumentProcessingException(
                    "KnowledgeDocument.TextlessPdf",
                    "The PDF contains no extractable text. OCR is not supported.");
            }

            EmbeddingBatch embeddingBatch =
                await embeddingClient.EmbedAsync(
                    textChunks.Select(chunk => chunk.Content).ToArray(),
                    cancellationToken);
            if (embeddingBatch.Embeddings.Count != textChunks.Count)
            {
                throw new KnowledgeDocumentProcessingException(
                    "KnowledgeDocument.EmbeddingCountMismatch",
                    "The embedding provider returned an invalid result count.");
            }

            KnowledgeChunkIndexEntry[] indexEntries = textChunks
                .Select((chunk, index) =>
                    new KnowledgeChunkIndexEntry(
                        Guid.NewGuid(),
                        index,
                        chunk.PageNumber,
                        chunk.Content,
                        embeddingBatch.Embeddings[index]))
                .ToArray();

            await indexRepository.PublishAsync(
                document,
                indexEntries,
                embeddingBatch.Model,
                embeddingBatch.Dimensions,
                timeProvider.GetUtcNow(),
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (KnowledgeAiUnavailableException)
        {
            await MarkFailedAsync(
                document,
                "KnowledgeDocument.EmbeddingProviderUnavailable",
                "The local embedding model is unavailable.",
                cancellationToken);
        }
        catch (KnowledgeDocumentProcessingException exception)
        {
            await MarkFailedAsync(
                document,
                exception.Code,
                exception.SafeMessage,
                cancellationToken);
        }
        catch (Exception)
        {
            await MarkFailedAsync(
                document,
                "KnowledgeDocument.ProcessingFailed",
                "Document processing failed. You can retry the document.",
                cancellationToken);
        }
    }

    private async Task MarkFailedAsync(
        KnowledgeDocument document,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        document.MarkFailed(
            errorCode,
            errorMessage,
            timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
