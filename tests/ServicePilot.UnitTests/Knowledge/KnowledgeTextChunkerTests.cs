using ServicePilot.Application.Knowledge;

namespace ServicePilot.UnitTests.Knowledge;

public sealed class KnowledgeTextChunkerTests
{
    [Fact]
    public void Chunk_ShouldNeverCrossPageBoundary()
    {
        KnowledgeTextChunker chunker = new();
        ExtractedPdfPage[] pages =
        [
            new(1, string.Join(' ', Enumerable.Repeat("first", 500))),
            new(2, string.Join(' ', Enumerable.Repeat("second", 500)))
        ];

        IReadOnlyList<KnowledgeTextChunk> chunks = chunker.Chunk(pages);

        Assert.All(
            chunks.Where(chunk => chunk.PageNumber == 1),
            chunk => Assert.DoesNotContain("second", chunk.Content));
        Assert.All(
            chunks.Where(chunk => chunk.PageNumber == 2),
            chunk => Assert.DoesNotContain("first", chunk.Content));
    }

    [Fact]
    public void Chunk_ShouldRespectMaximumAndCarryOverlap()
    {
        KnowledgeTextChunker chunker = new();
        string text = string.Join(
            ' ',
            Enumerable.Range(0, 900).Select(index => $"word{index:D4}"));

        IReadOnlyList<KnowledgeTextChunk> chunks = chunker.Chunk(
            [new ExtractedPdfPage(7, text)]);

        Assert.True(chunks.Count > 1);
        Assert.All(
            chunks,
            chunk => Assert.InRange(
                chunk.Content.Length,
                1,
                KnowledgeTextChunker.MaximumCharacters));
        string[] firstWords = chunks[0].Content.Split(' ');
        string[] secondWords = chunks[1].Content.Split(' ');
        Assert.Contains(firstWords[^1], secondWords);
        Assert.All(chunks, chunk => Assert.Equal(7, chunk.PageNumber));
    }
}