using System.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Npgsql;

using Pgvector;

using ServicePilot.Application.Knowledge;
using ServicePilot.Domain.Knowledge;
using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.Infrastructure.Knowledge;

internal sealed class KnowledgeDocumentIndexRepository(
    ServicePilotDbContext dbContext)
    : IKnowledgeDocumentIndexRepository
{
    private const int RequiredDimensions = 1024;

    public async Task PublishAsync(
        KnowledgeDocument document,
        IReadOnlyList<KnowledgeChunkIndexEntry> chunks,
        string embeddingModel,
        int embeddingDimensions,
        DateTimeOffset completedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0
            || embeddingDimensions != RequiredDimensions
            || chunks.Any(chunk =>
                chunk.Embedding.Count != RequiredDimensions))
        {
            throw new KnowledgeDocumentProcessingException(
                "KnowledgeDocument.InvalidEmbeddingDimensions",
                "Document embeddings do not match the configured index.");
        }

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);
        try
        {
            NpgsqlConnection connection = (NpgsqlConnection)
                dbContext.Database.GetDbConnection();
            NpgsqlTransaction npgsqlTransaction = (NpgsqlTransaction)
                transaction.GetDbTransaction();

            await DeleteExistingAsync(
                connection,
                npgsqlTransaction,
                document.OrganizationId,
                document.Id,
                cancellationToken);
            await InsertChunksAsync(
                connection,
                npgsqlTransaction,
                document,
                chunks,
                embeddingModel,
                embeddingDimensions,
                completedAtUtc,
                cancellationToken);

            document.MarkReady(completedAtUtc);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            await dbContext.Entry(document).ReloadAsync(
                CancellationToken.None);
            throw;
        }
    }

    private static async Task DeleteExistingAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid organizationId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = new(
            """
            DELETE FROM knowledge_document_chunks
            WHERE organization_id = $1 AND document_id = $2
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue(organizationId);
        command.Parameters.AddWithValue(documentId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertChunksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        KnowledgeDocument document,
        IReadOnlyList<KnowledgeChunkIndexEntry> chunks,
        string embeddingModel,
        int embeddingDimensions,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = new(
            """
            INSERT INTO knowledge_document_chunks (
                id, organization_id, document_id, chunk_index,
                page_number, content, embedding_model,
                embedding_dimensions, embedding, created_at_utc)
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)
            """,
            connection,
            transaction);
        for (int index = 0; index < 10; index++)
        {
            command.Parameters.Add(new NpgsqlParameter());
        }

        foreach (KnowledgeChunkIndexEntry chunk in chunks)
        {
            command.Parameters[0].Value = chunk.Id;
            command.Parameters[1].Value = document.OrganizationId;
            command.Parameters[2].Value = document.Id;
            command.Parameters[3].Value = chunk.ChunkIndex;
            command.Parameters[4].Value = chunk.PageNumber;
            command.Parameters[5].Value = chunk.Content;
            command.Parameters[6].Value = embeddingModel;
            command.Parameters[7].Value = embeddingDimensions;
            command.Parameters[8].Value = new Vector(
                chunk.Embedding.ToArray());
            command.Parameters[9].Value = createdAtUtc;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}