using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Abstractions.Persistence.Exceptions;
using ServicePilot.Application.Common;
using ServicePilot.Domain.Knowledge;

namespace ServicePilot.Application.Knowledge;

public sealed class KnowledgeDocumentService(
    ICurrentUserContext currentUser,
    IKnowledgeDocumentRepository repository,
    IKnowledgeDocumentStorage storage,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    KnowledgeDocumentLimits limits)
{
    public const long MaximumPdfBytes = 20 * 1024 * 1024;

    public async Task<Result<KnowledgeDocumentResponse>> UploadAsync(
        UploadKnowledgeDocument upload,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = Validate(upload);
        if (validationError is not null)
        {
            return Result<KnowledgeDocumentResponse>.Failure(validationError);
        }

        if (!TryParseDefined(upload.DocumentType, out KnowledgeDocumentType type))
        {
            return Result<KnowledgeDocumentResponse>.Failure(
                KnowledgeDocumentErrors.InvalidType);
        }

        KnowledgeDocumentUsage usage =
            await repository.GetActiveUsageAsync(
                currentUser.OrganizationId,
                cancellationToken);
        if (usage.DocumentCount
            >= limits.MaximumDocumentsPerOrganization)
        {
            return Result<KnowledgeDocumentResponse>.Failure(
                KnowledgeDocumentErrors.DocumentLimitReached);
        }

        if (upload.SizeBytes
            > limits.MaximumStorageBytesPerOrganization
                - usage.TotalSizeBytes)
        {
            return Result<KnowledgeDocumentResponse>.Failure(
                KnowledgeDocumentErrors.StorageLimitReached);
        }

        KnowledgeDocumentAccessScope? requestedScope = null;
        if (!string.IsNullOrWhiteSpace(upload.AccessScope))
        {
            if (!TryParseDefined(
                    upload.AccessScope,
                    out KnowledgeDocumentAccessScope parsedScope))
            {
                return Result<KnowledgeDocumentResponse>.Failure(
                    KnowledgeDocumentErrors.InvalidAccessScope);
            }

            requestedScope = parsedScope;
        }

        Guid documentId = Guid.NewGuid();
        StoredKnowledgeFile storedFile;

        try
        {
            storedFile = await storage.SavePdfAsync(
                currentUser.OrganizationId,
                documentId,
                upload.Content,
                MaximumPdfBytes,
                cancellationToken);
        }
        catch (InvalidDataException)
        {
            return Result<KnowledgeDocumentResponse>.Failure(
                KnowledgeDocumentErrors.InvalidPdf);
        }

        if (await repository.ActiveChecksumExistsAsync(
            currentUser.OrganizationId,
            storedFile.ChecksumSha256,
            cancellationToken))
        {
            await storage.DeleteAsync(
                storedFile.StorageKey,
                cancellationToken);
            return Result<KnowledgeDocumentResponse>.Failure(
                KnowledgeDocumentErrors.DuplicateContent);
        }

        KnowledgeDocumentAccessScope scope = requestedScope
            ?? KnowledgeDocument.DefaultScopeFor(type);
        KnowledgeDocument document;

        try
        {
            document = new KnowledgeDocument(
                documentId,
                currentUser.OrganizationId,
                currentUser.UserId,
                Path.GetFileName(upload.OriginalFileName),
                "application/pdf",
                storedFile.StorageKey,
                storedFile.ChecksumSha256,
                storedFile.SizeBytes,
                type,
                scope,
                timeProvider.GetUtcNow());
        }
        catch (ArgumentException)
        {
            await storage.DeleteAsync(
                storedFile.StorageKey,
                cancellationToken);
            return Result<KnowledgeDocumentResponse>.Failure(
                KnowledgeDocumentErrors.InvalidAccessScope);
        }

        repository.Add(document);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException exception)
            when (exception.ConstraintName
                == "ux_knowledge_documents_organization_checksum")
        {
            await storage.DeleteAsync(
                storedFile.StorageKey,
                cancellationToken);
            return Result<KnowledgeDocumentResponse>.Failure(
                KnowledgeDocumentErrors.DuplicateContent);
        }
        catch
        {
            await storage.DeleteAsync(
                storedFile.StorageKey,
                cancellationToken);
            throw;
        }

        return Result<KnowledgeDocumentResponse>.Success(Map(document));
    }

    public async Task<IReadOnlyList<KnowledgeDocumentResponse>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<KnowledgeDocumentAccessScope> scopes =
            KnowledgeAccessPolicy.AllowedScopes(currentUser.Role);
        IReadOnlyList<KnowledgeDocument> documents =
            await repository.ListAsync(
                currentUser.OrganizationId,
                scopes,
                cancellationToken);

        return documents.Select(Map).ToArray();
    }

    public async Task<Result<KnowledgeDocumentDownload>> OpenAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        KnowledgeDocument? document = await repository.GetByIdAsync(
            currentUser.OrganizationId,
            documentId,
            cancellationToken);

        if (document is null
            || document.Status == KnowledgeDocumentStatus.Deleted
            || !document.CanBeAccessedByRole(currentUser.Role))
        {
            return Result<KnowledgeDocumentDownload>.Failure(
                KnowledgeDocumentErrors.NotFound);
        }

        Stream content = await storage.OpenReadAsync(
            document.StorageKey,
            cancellationToken);
        return Result<KnowledgeDocumentDownload>.Success(
            new KnowledgeDocumentDownload(
                content,
                document.ContentType,
                document.OriginalFileName));
    }

    public async Task<Result<KnowledgeDocumentResponse>> RetryAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        KnowledgeDocument? document = await repository.GetByIdAsync(
            currentUser.OrganizationId,
            documentId,
            cancellationToken);
        if (document is null)
        {
            return Result<KnowledgeDocumentResponse>.Failure(
                KnowledgeDocumentErrors.NotFound);
        }

        try
        {
            document.Retry(timeProvider.GetUtcNow());
        }
        catch (InvalidOperationException)
        {
            return Result<KnowledgeDocumentResponse>.Failure(
                KnowledgeDocumentErrors.InvalidRetry);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<KnowledgeDocumentResponse>.Success(Map(document));
    }

    public async Task<Result<KnowledgeDocumentResponse>> ChangeAccessScopeAsync(
        Guid documentId,
        ChangeKnowledgeDocumentAccessScope request,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseDefined(
                request.AccessScope,
                out KnowledgeDocumentAccessScope accessScope))
        {
            return Result<KnowledgeDocumentResponse>.Failure(
                KnowledgeDocumentErrors.InvalidAccessScope);
        }

        KnowledgeDocument? document = await repository.GetByIdAsync(
            currentUser.OrganizationId,
            documentId,
            cancellationToken);
        if (document is null
            || document.Status == KnowledgeDocumentStatus.Deleted)
        {
            return Result<KnowledgeDocumentResponse>.Failure(
                KnowledgeDocumentErrors.NotFound);
        }

        try
        {
            document.ChangeAccessScope(
                accessScope,
                timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ArgumentException)
        {
            return Result<KnowledgeDocumentResponse>.Failure(
                KnowledgeDocumentErrors.InvalidAccessScope);
        }
        catch (InvalidOperationException)
        {
            return Result<KnowledgeDocumentResponse>.Failure(
                KnowledgeDocumentErrors.NotFound);
        }
        catch (ConcurrencyViolationException)
        {
            return Result<KnowledgeDocumentResponse>.Failure(
                KnowledgeDocumentErrors.ConcurrentChange);
        }

        return Result<KnowledgeDocumentResponse>.Success(Map(document));
    }

    public async Task<Result> DeleteAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        KnowledgeDocument? document = await repository.GetByIdAsync(
            currentUser.OrganizationId,
            documentId,
            cancellationToken);
        if (document is null
            || document.Status == KnowledgeDocumentStatus.Deleted)
        {
            return Result.Failure(KnowledgeDocumentErrors.NotFound);
        }

        document.MarkDeleted(timeProvider.GetUtcNow());
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyViolationException)
        {
            return Result.Failure(
                KnowledgeDocumentErrors.ConcurrentChange);
        }

        return Result.Success();
    }

    private static Error? Validate(UploadKnowledgeDocument upload)
    {
        string fileName = Path.GetFileName(upload.OriginalFileName);
        if (string.IsNullOrWhiteSpace(fileName)
            || fileName.Length > KnowledgeDocument.MaxOriginalFileNameLength
            || fileName.Any(char.IsControl))
        {
            return KnowledgeDocumentErrors.InvalidFileName;
        }

        if (upload.SizeBytes <= 0
            || upload.SizeBytes > MaximumPdfBytes)
        {
            return upload.SizeBytes > MaximumPdfBytes
                ? KnowledgeDocumentErrors.FileTooLarge
                : KnowledgeDocumentErrors.InvalidPdf;
        }

        if (!upload.ContentType.Equals(
                "application/pdf",
                StringComparison.OrdinalIgnoreCase)
            || !Path.GetExtension(fileName).Equals(
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            return KnowledgeDocumentErrors.InvalidPdf;
        }

        return null;
    }

    private static bool TryParseDefined<TEnum>(
        string? value,
        out TEnum parsed)
        where TEnum : struct, Enum =>
        Enum.TryParse(value, true, out parsed)
        && Enum.IsDefined(parsed);

    private static KnowledgeDocumentResponse Map(
        KnowledgeDocument document) =>
        new(
            document.Id,
            document.OriginalFileName,
            document.SizeBytes,
            document.Type.ToString(),
            document.AccessScope.ToString(),
            document.Status.ToString(),
            document.ProcessingAttemptCount,
            document.LastErrorCode,
            document.LastErrorMessage,
            document.CreatedAtUtc,
            document.UpdatedAtUtc);
}
