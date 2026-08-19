using ServicePilot.Application.Knowledge;

using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace ServicePilot.Infrastructure.Knowledge;

internal sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    public Task<IReadOnlyList<ExtractedPdfPage>> ExtractAsync(
        Stream pdf,
        int maximumPages,
        int maximumCharacters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using PdfDocument document = PdfDocument.Open(pdf);
            if (document.NumberOfPages > maximumPages)
            {
                throw new KnowledgeDocumentProcessingException(
                    "KnowledgeDocument.TooManyPages",
                    $"The PDF exceeds the {maximumPages} page limit.");
            }

            List<ExtractedPdfPage> pages = [];
            int totalCharacters = 0;
            for (int pageNumber = 1;
                pageNumber <= document.NumberOfPages;
                pageNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Page page = document.GetPage(pageNumber);
                string text = ContentOrderTextExtractor.GetText(page);
                totalCharacters = checked(totalCharacters + text.Length);
                if (totalCharacters > maximumCharacters)
                {
                    throw new KnowledgeDocumentProcessingException(
                        "KnowledgeDocument.ExtractedTextTooLarge",
                        "The extracted PDF text exceeds the processing limit.");
                }

                pages.Add(new ExtractedPdfPage(pageNumber, text));
            }

            return Task.FromResult<
                IReadOnlyList<ExtractedPdfPage>>(pages);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (KnowledgeDocumentProcessingException)
        {
            throw;
        }
        catch (Exception exception)
        {
            bool encrypted = exception.GetType().Name.Contains(
                "Encrypted",
                StringComparison.OrdinalIgnoreCase);
            throw new KnowledgeDocumentProcessingException(
                encrypted
                    ? "KnowledgeDocument.EncryptedPdf"
                    : "KnowledgeDocument.MalformedPdf",
                encrypted
                    ? "Encrypted PDFs are not supported."
                    : "The PDF could not be read.",
                exception);
        }
    }
}