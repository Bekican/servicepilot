namespace ServicePilot.Application.Knowledge;

public interface IPdfTextExtractor
{
    Task<IReadOnlyList<ExtractedPdfPage>> ExtractAsync(
        Stream pdf,
        int maximumPages,
        int maximumCharacters,
        CancellationToken cancellationToken = default);
}

public sealed record ExtractedPdfPage(
    int PageNumber,
    string Text);

public sealed class KnowledgeDocumentProcessingException(
    string code,
    string safeMessage,
    Exception? innerException = null)
    : Exception(safeMessage, innerException)
{
    public string Code { get; } = code;
    public string SafeMessage { get; } = safeMessage;
}