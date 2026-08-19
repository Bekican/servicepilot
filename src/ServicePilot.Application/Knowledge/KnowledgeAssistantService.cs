using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Common;
using ServicePilot.Domain.Knowledge;

namespace ServicePilot.Application.Knowledge;

public sealed class KnowledgeAssistantService(
    ICurrentUserContext currentUser,
    ITextEmbeddingClient embeddingClient,
    IKnowledgeRetrievalRepository retrievalRepository,
    IGroundedAnswerGenerator answerGenerator)
{
    public const int MaximumQuestionLength = 2000;
    public const string NoAnswerMessage =
        "Bu bilgi yüklenen belgelerde bulunamadı.";

    private const int CandidateCount = 12;
    private const int MaximumSources = 8;
    private const int MaximumChunksPerDocument = 3;

    public async Task<Result<KnowledgeAnswer>> AskAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        string normalizedQuestion = question?.Trim() ?? string.Empty;
        if (normalizedQuestion.Length is 0 or > MaximumQuestionLength)
        {
            return Result<KnowledgeAnswer>.Failure(
                KnowledgeAssistantErrors.InvalidQuestion);
        }

        try
        {
            EmbeddingBatch embeddingBatch = await embeddingClient.EmbedAsync(
                [BuildEmbeddingInput(normalizedQuestion)],
                cancellationToken);
            IReadOnlyList<float> queryEmbedding =
                AssertSingleEmbedding(embeddingBatch);

            IReadOnlyCollection<KnowledgeDocumentAccessScope> scopes =
                KnowledgeAccessPolicy.AllowedScopes(currentUser.Role);
            IReadOnlyList<RetrievedKnowledgeChunk> candidates =
                await retrievalRepository.SearchAsync(
                    currentUser.OrganizationId,
                    scopes,
                    queryEmbedding,
                    embeddingBatch.Model,
                    embeddingBatch.Dimensions,
                    CandidateCount,
                    cancellationToken);
            IReadOnlyList<GroundingSource> sources = SelectSources(candidates);
            if (sources.Count == 0)
            {
                return Result<KnowledgeAnswer>.Success(NoAnswer());
            }

            GeneratedGroundedAnswer generated =
                await answerGenerator.GenerateAsync(
                    normalizedQuestion,
                    sources,
                    cancellationToken);
            return Result<KnowledgeAnswer>.Success(
                ValidateAndMap(generated, sources));
        }
        catch (KnowledgeAiUnavailableException)
        {
            return Result<KnowledgeAnswer>.Failure(
                KnowledgeAssistantErrors.ProviderUnavailable);
        }
    }

    private static string BuildEmbeddingInput(string question) =>
        "Instruct: Retrieve passages from Turkish technical service "
        + "reports, procedures, manuals, and warranty documents that "
        + "answer the user's question.\nQuery: " + question;

    private static IReadOnlyList<float> AssertSingleEmbedding(
        EmbeddingBatch batch)
    {
        if (batch.Embeddings.Count != 1
            || batch.Dimensions <= 0
            || batch.Embeddings[0].Count != batch.Dimensions)
        {
            throw new KnowledgeAiUnavailableException(
                "The embedding provider returned an invalid response.");
        }

        return batch.Embeddings[0];
    }

    private static IReadOnlyList<GroundingSource> SelectSources(
        IReadOnlyList<RetrievedKnowledgeChunk> candidates)
    {
        Dictionary<Guid, int> documentCounts = [];
        List<GroundingSource> selected = [];
        foreach (RetrievedKnowledgeChunk candidate in candidates)
        {
            int count = documentCounts.GetValueOrDefault(
                candidate.DocumentId);
            if (count >= MaximumChunksPerDocument)
            {
                continue;
            }

            selected.Add(new GroundingSource(
                $"S{selected.Count + 1}",
                candidate.DocumentId,
                candidate.OriginalFileName,
                candidate.PageNumber,
                candidate.Content));
            documentCounts[candidate.DocumentId] = count + 1;
            if (selected.Count == MaximumSources)
            {
                break;
            }
        }

        return selected;
    }

    private static KnowledgeAnswer ValidateAndMap(
        GeneratedGroundedAnswer generated,
        IReadOnlyList<GroundingSource> sources)
    {
        if (generated.InsufficientEvidence)
        {
            return NoAnswer();
        }

        Dictionary<string, GroundingSource> supplied = sources.ToDictionary(
            source => source.SourceId,
            StringComparer.Ordinal);
        string[] citationIds = (generated.Citations ?? [])
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        bool invalid = string.IsNullOrWhiteSpace(generated.Answer)
            || citationIds.Length == 0
            || citationIds.Any(id => !supplied.ContainsKey(id))
            || citationIds.Any(id => !generated.Answer.Contains(
                $"[{id}]",
                StringComparison.Ordinal));
        if (invalid)
        {
            return NoAnswer();
        }

        KnowledgeCitation[] citations = citationIds
            .Select(id => supplied[id])
            .Select(source => new KnowledgeCitation(
                source.SourceId,
                source.DocumentId,
                source.OriginalFileName,
                source.PageNumber,
                $"/api/knowledge/documents/{source.DocumentId}/content"
                    + $"#page={source.PageNumber}"))
            .ToArray();
        return new KnowledgeAnswer(
            generated.Answer.Trim(),
            false,
            citations);
    }

    private static KnowledgeAnswer NoAnswer() =>
        new(NoAnswerMessage, true, []);
}
