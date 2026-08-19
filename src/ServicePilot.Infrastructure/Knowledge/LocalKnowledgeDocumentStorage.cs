using System.Security.Cryptography;

using ServicePilot.Application.Knowledge;

namespace ServicePilot.Infrastructure.Knowledge;

internal sealed class LocalKnowledgeDocumentStorage(
    KnowledgeStorageOptions options)
    : IKnowledgeDocumentStorage
{
    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();

    public async Task<StoredKnowledgeFile> SavePdfAsync(
        Guid organizationId,
        Guid documentId,
        Stream content,
        long maximumBytes,
        CancellationToken cancellationToken = default)
    {
        string storageKey = FormattableString.Invariant(
            $"knowledge/{organizationId:D}/{documentId:D}/source.pdf");
        string filePath = ResolvePath(storageKey);
        Directory.CreateDirectory(
            Path.GetDirectoryName(filePath)!);

        try
        {
            await using FileStream destination = new(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous
                    | FileOptions.SequentialScan);
            using IncrementalHash hash = IncrementalHash.CreateHash(
                HashAlgorithmName.SHA256);

            byte[] buffer = new byte[81920];
            long totalBytes = 0;
            int signatureBytes = 0;
            byte[] signature = new byte[PdfSignature.Length];

            while (true)
            {
                int bytesRead = await content.ReadAsync(
                    buffer,
                    cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                totalBytes += bytesRead;
                if (totalBytes > maximumBytes)
                {
                    throw new InvalidDataException(
                        "PDF exceeds the configured size limit");
                }

                if (signatureBytes < signature.Length)
                {
                    int bytesToCopy = Math.Min(
                        signature.Length - signatureBytes,
                        bytesRead);
                    buffer.AsSpan(0, bytesToCopy).CopyTo(
                        signature.AsSpan(signatureBytes));
                    signatureBytes += bytesToCopy;
                }

                hash.AppendData(buffer, 0, bytesRead);
                await destination.WriteAsync(
                    buffer.AsMemory(0, bytesRead),
                    cancellationToken);
            }

            if (signatureBytes != PdfSignature.Length
                || !signature.AsSpan().SequenceEqual(PdfSignature))
            {
                throw new InvalidDataException(
                    "File does not have a PDF signature");
            }

            return new StoredKnowledgeFile(
                storageKey,
                Convert.ToHexString(hash.GetHashAndReset())
                    .ToLowerInvariant(),
                totalBytes);
        }
        catch
        {
            File.Delete(filePath);
            DeleteEmptyParentDirectories(filePath);
            throw;
        }
    }

    public Task<Stream> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Stream stream = new FileStream(
            ResolvePath(storageKey),
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous
                | FileOptions.SequentialScan);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string filePath = ResolvePath(storageKey);
        File.Delete(filePath);
        DeleteEmptyParentDirectories(filePath);
        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        string normalizedKey = storageKey.Replace(
            '/',
            Path.DirectorySeparatorChar);
        string fullPath = Path.GetFullPath(
            Path.Combine(options.RootPath, normalizedKey));
        string root = options.RootPath.TrimEnd(
            Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(
            root,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Knowledge storage key escapes the configured root");
        }

        return fullPath;
    }

    private void DeleteEmptyParentDirectories(string filePath)
    {
        DirectoryInfo? directory =
            Directory.GetParent(filePath);
        string root = options.RootPath.TrimEnd(
            Path.DirectorySeparatorChar);

        while (directory is not null
            && !directory.FullName.Equals(
                root,
                StringComparison.OrdinalIgnoreCase)
            && !directory.EnumerateFileSystemInfos().Any())
        {
            DirectoryInfo? parent = directory.Parent;
            directory.Delete();
            directory = parent;
        }
    }
}
