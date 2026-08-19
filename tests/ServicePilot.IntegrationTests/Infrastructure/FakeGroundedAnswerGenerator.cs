using ServicePilot.Application.Knowledge;

namespace ServicePilot.IntegrationTests.Infrastructure;

internal sealed class FakeGroundedAnswerGenerator
    : IGroundedAnswerGenerator
{
    public Task<GeneratedGroundedAnswer> GenerateAsync(
        string question,
        IReadOnlyList<GroundingSource> sources,
        CancellationToken cancellationToken = default)
    {
        GroundingSource source = sources[0];
        return Task.FromResult(new GeneratedGroundedAnswer(
            $"Test cevabı. [{source.SourceId}]",
            [source.SourceId],
            false));
    }
}
