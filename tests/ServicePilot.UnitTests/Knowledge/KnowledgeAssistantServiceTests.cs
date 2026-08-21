using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Knowledge;
using ServicePilot.Domain.Knowledge;
using ServicePilot.Domain.Users;

namespace ServicePilot.UnitTests.Knowledge;

public sealed class KnowledgeAssistantServiceTests
{
    [Fact]
    public async Task Ask_ShouldReturnValidatedClickableCitation()
    {
        Guid documentId = Guid.NewGuid();
        FakeRetrievalRepository repository = new(
        [
            new RetrievedKnowledgeChunk(
                documentId,
                "bakim.pdf",
                4,
                0,
                "Filtre altı ayda bir kontrol edilir.",
                0.05)
        ]);
        KnowledgeAssistantService service = CreateService(
            repository,
            new FakeAnswerGenerator(
                new GeneratedGroundedAnswer(
                    "Filtre altı ayda bir kontrol edilir. [S1]",
                    ["S1"],
                    false)));

        var result = await service.AskAsync(
            "Filtre ne zaman kontrol edilir?");

        Assert.True(result.IsSuccess);
        KnowledgeCitation citation = Assert.Single(
            result.Value.Citations);
        Assert.Equal(documentId, citation.DocumentId);
        Assert.EndsWith("/content#page=4", citation.ContentUrl);
        Assert.False(result.Value.InsufficientEvidence);
    }

    [Fact]
    public async Task Ask_ShouldRejectHallucinatedCitation()
    {
        KnowledgeAssistantService service = CreateService(
            new FakeRetrievalRepository(
            [
                new RetrievedKnowledgeChunk(
                    Guid.NewGuid(),
                    "manual.pdf",
                    1,
                    0,
                    "Kaynak metni",
                    0.1)
            ]),
            new FakeAnswerGenerator(
                new GeneratedGroundedAnswer(
                    "Uydurma cevap. [S99]",
                    ["S99"],
                    false)));

        var result = await service.AskAsync("Bir soru");

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.InsufficientEvidence);
        Assert.Empty(result.Value.Citations);
        Assert.Equal(
            KnowledgeAssistantService.NoAnswerMessage,
            result.Value.Answer);
    }

    [Fact]
    public async Task Ask_ShouldAppendMissingCitationMarker()
    {
        Guid documentId = Guid.NewGuid();
        KnowledgeAssistantService service = CreateService(
            new FakeRetrievalRepository(
            [
                new RetrievedKnowledgeChunk(
                    documentId,
                    "bakim.pdf",
                    2,
                    0,
                    "Filtre alti ayda bir kontrol edilir.",
                    0.05)
            ]),
            new FakeAnswerGenerator(
                new GeneratedGroundedAnswer(
                    "Filtre alti ayda bir kontrol edilir.",
                    ["S1"],
                    false)));

        var result = await service.AskAsync(
            "Kombi filtresini ne zaman kontrol etmeliyim?");

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.InsufficientEvidence);
        Assert.EndsWith("[S1]", result.Value.Answer);
        Assert.Single(result.Value.Citations);
    }

    [Fact]
    public async Task Ask_ShouldApplyTechnicianScopeAndTenant()
    {
        FakeCurrentUser currentUser = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            UserRoles.Technician);
        FakeRetrievalRepository repository = new([]);
        KnowledgeAssistantService service = new(
            currentUser,
            new FakeEmbeddingClient(),
            repository,
            new FakeAnswerGenerator(
                new GeneratedGroundedAnswer("", [], true)));

        await service.AskAsync("Bir soru");

        Assert.Equal(currentUser.OrganizationId, repository.OrganizationId);
        Assert.Equal(
            [KnowledgeDocumentAccessScope.Shared],
            repository.AllowedScopes);
    }

    private static KnowledgeAssistantService CreateService(
        IKnowledgeRetrievalRepository repository,
        IGroundedAnswerGenerator generator) =>
        new(
            new FakeCurrentUser(
                Guid.NewGuid(),
                Guid.NewGuid(),
                UserRoles.Owner),
            new FakeEmbeddingClient(),
            repository,
            generator);

    private sealed record FakeCurrentUser(
        Guid UserId,
        Guid OrganizationId,
        string Role) : ICurrentUserContext;

    private sealed class FakeEmbeddingClient : ITextEmbeddingClient
    {
        public Task<EmbeddingBatch> EmbedAsync(
            IReadOnlyList<string> inputs,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new EmbeddingBatch(
                "fake-model",
                3,
                [[1, 0, 0]]));
    }

    private sealed class FakeRetrievalRepository(
        IReadOnlyList<RetrievedKnowledgeChunk> results)
        : IKnowledgeRetrievalRepository
    {
        public Guid OrganizationId { get; private set; }
        public IReadOnlyCollection<KnowledgeDocumentAccessScope>
            AllowedScopes { get; private set; } = [];

        public Task<IReadOnlyList<RetrievedKnowledgeChunk>> SearchAsync(
            Guid organizationId,
            IReadOnlyCollection<KnowledgeDocumentAccessScope> allowedScopes,
            IReadOnlyList<float> queryEmbedding,
            string embeddingModel,
            int embeddingDimensions,
            int candidateCount,
            CancellationToken cancellationToken = default)
        {
            OrganizationId = organizationId;
            AllowedScopes = allowedScopes;
            return Task.FromResult(results);
        }
    }

    private sealed class FakeAnswerGenerator(
        GeneratedGroundedAnswer answer)
        : IGroundedAnswerGenerator
    {
        public Task<GeneratedGroundedAnswer> GenerateAsync(
            string question,
            IReadOnlyList<GroundingSource> sources,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(answer);
    }
}
