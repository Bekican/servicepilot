using Npgsql;

using Pgvector;

using ServicePilot.Application.Knowledge;
using ServicePilot.Domain.Knowledge;

namespace ServicePilot.Infrastructure.Knowledge;

internal sealed class KnowledgeRetrievalRepository(
    NpgsqlDataSource dataSource)
    : IKnowledgeRetrievalRepository
{
    public async Task<IReadOnlyList<RetrievedKnowledgeChunk>> SearchAsync(
        Guid organizationId,
        IReadOnlyCollection<KnowledgeDocumentAccessScope> allowedScopes,
        IReadOnlyList<float> queryEmbedding,
        string embeddingModel,
        int embeddingDimensions,
        int candidateCount,
        CancellationToken cancellationToken = default)
    {
        if (allowedScopes.Count == 0)
        {
            return [];
        }

        await using NpgsqlConnection connection =
            await dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT d.id,
                   d.original_file_name,
                   c.page_number,
                   c.chunk_index,
                   c.content,
                   c.embedding <=> $1 AS cosine_distance
            FROM knowledge_document_chunks AS c
            INNER JOIN knowledge_documents AS d
                ON d.organization_id = c.organization_id
               AND d.id = c.document_id
            WHERE c.organization_id = $2
              AND d.organization_id = $2
              AND d.status = 'Ready'
              AND d.access_scope = ANY($3)
              AND c.embedding_model = $4
              AND c.embedding_dimensions = $5
            ORDER BY c.embedding <=> $1, c.id
            LIMIT $6
            """;
        command.Parameters.AddWithValue(
            new Vector(queryEmbedding.ToArray()));
        command.Parameters.AddWithValue(organizationId);
        command.Parameters.AddWithValue(
            allowedScopes.Select(scope => scope.ToString()).ToArray());
        command.Parameters.AddWithValue(embeddingModel);
        command.Parameters.AddWithValue(embeddingDimensions);
        command.Parameters.AddWithValue(Math.Clamp(candidateCount, 1, 50));

        List<RetrievedKnowledgeChunk> results = [];
        await using NpgsqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new RetrievedKnowledgeChunk(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetInt32(3),
                reader.GetString(4),
                reader.GetDouble(5)));
        }

        return results;
    }
}
