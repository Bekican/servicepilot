using System.Diagnostics;

using ServicePilot.Application.Knowledge;

namespace ServicePilot.Infrastructure.Knowledge;

internal sealed class OcrFallbackPdfTextExtractor(
    PdfPigTextExtractor primary,
    KnowledgeOcrOptions options) : IPdfTextExtractor
{
    private const int MinimumUsefulCharactersPerPage = 20;

    public async Task<IReadOnlyList<ExtractedPdfPage>> ExtractAsync(
        Stream pdf,
        int maximumPages,
        int maximumCharacters,
        CancellationToken cancellationToken = default)
    {
        string temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            $"servicepilot-ocr-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);
        string pdfPath = Path.Combine(temporaryDirectory, "source.pdf");

        try
        {
            await using (FileStream target = File.Create(pdfPath))
            {
                await pdf.CopyToAsync(target, cancellationToken);
            }

            IReadOnlyList<ExtractedPdfPage> extracted;
            await using (FileStream source = File.OpenRead(pdfPath))
            {
                extracted = await primary.ExtractAsync(
                    source,
                    maximumPages,
                    maximumCharacters,
                    cancellationToken);
            }

            if (!options.Enabled)
            {
                return extracted;
            }

            List<ExtractedPdfPage> pages = new(extracted.Count);
            int totalCharacters = 0;
            foreach (ExtractedPdfPage page in extracted)
            {
                string text = page.Text;
                if (text.Count(character => !char.IsWhiteSpace(character))
                    < MinimumUsefulCharactersPerPage)
                {
                    text = await ExtractPageWithOcrAsync(
                        pdfPath,
                        temporaryDirectory,
                        page.PageNumber,
                        cancellationToken);
                }

                totalCharacters = checked(totalCharacters + text.Length);
                if (totalCharacters > maximumCharacters)
                {
                    throw new KnowledgeDocumentProcessingException(
                        "KnowledgeDocument.ExtractedTextTooLarge",
                        "The extracted PDF text exceeds the processing limit.");
                }

                pages.Add(new ExtractedPdfPage(page.PageNumber, text));
            }

            return pages;
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
            throw new KnowledgeDocumentProcessingException(
                "KnowledgeDocument.OcrFailed",
                "The scanned PDF could not be read with OCR.",
                exception);
        }
        finally
        {
            try
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
            catch (IOException)
            {
                // The operating system will eventually clear its temporary area.
            }
            catch (UnauthorizedAccessException)
            {
                // Do not turn successful indexing into a failure during cleanup.
            }
        }
    }

    private async Task<string> ExtractPageWithOcrAsync(
        string pdfPath,
        string temporaryDirectory,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        string imageRoot = Path.Combine(
            temporaryDirectory,
            $"page-{pageNumber}");
        await RunAsync(
            "pdftoppm",
            [
                "-f", pageNumber.ToString(),
                "-l", pageNumber.ToString(),
                "-singlefile",
                "-r", options.Dpi.ToString(),
                "-png",
                pdfPath,
                imageRoot
            ],
            captureStandardOutput: false,
            cancellationToken);

        return await RunAsync(
            "tesseract",
            [
                $"{imageRoot}.png",
                "stdout",
                "-l", options.Languages,
                "--psm", "6"
            ],
            captureStandardOutput: true,
            cancellationToken);
    }

    private async Task<string> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        bool captureStandardOutput,
        CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = new(fileName)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = new() { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException(
                $"Could not start {fileName}.");
        }

        Task<string> outputTask = process.StandardOutput.ReadToEndAsync(
            cancellationToken);
        Task<string> errorTask = process.StandardError.ReadToEndAsync(
            cancellationToken);
        using CancellationTokenSource timeout =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);
        timeout.CancelAfter(options.PageTimeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(
                $"{fileName} exceeded the OCR page timeout.");
        }

        string output = await outputTask;
        string error = await errorTask;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{fileName} failed with exit code {process.ExitCode}: "
                + error[..Math.Min(error.Length, 300)]);
        }

        return captureStandardOutput ? output : string.Empty;
    }
}
