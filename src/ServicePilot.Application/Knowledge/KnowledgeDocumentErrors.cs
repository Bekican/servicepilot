using ServicePilot.Application.Common;

namespace ServicePilot.Application.Knowledge;

public static class KnowledgeDocumentErrors
{
    public static readonly Error NotFound = new(
        "KnowledgeDocument.NotFound",
        "Knowledge document was not found.");

    public static readonly Error InvalidPdf = Error.ForField(
        "KnowledgeDocument.InvalidPdf",
        "Only a valid PDF document can be uploaded.",
        "file",
        "InvalidPdf");

    public static readonly Error FileTooLarge = Error.ForField(
        "KnowledgeDocument.FileTooLarge",
        "The PDF exceeds the 20 MB upload limit.",
        "file",
        "FileTooLarge");

    public static readonly Error InvalidFileName = Error.ForField(
        "KnowledgeDocument.InvalidFileName",
        "The PDF filename is invalid.",
        "file",
        "InvalidFileName");

    public static readonly Error InvalidType = Error.ForField(
        "KnowledgeDocument.InvalidType",
        "The document type is invalid.",
        "documentType",
        "InvalidType");

    public static readonly Error InvalidAccessScope = Error.ForField(
        "KnowledgeDocument.InvalidAccessScope",
        "The access scope is invalid for this document.",
        "accessScope",
        "InvalidAccessScope");

    public static readonly Error DuplicateContent = new(
        "KnowledgeDocument.DuplicateContent",
        "This PDF is already registered in the organization.");

    public static readonly Error InvalidRetry = new(
        "KnowledgeDocument.InvalidRetry",
        "Only failed documents can be retried.");

    public static readonly Error DocumentLimitReached = new(
        "KnowledgeDocument.DocumentLimitReached",
        "The organization's document limit has been reached.");

    public static readonly Error StorageLimitReached = new(
        "KnowledgeDocument.StorageLimitReached",
        "The organization's document storage limit has been reached.");

    public static readonly Error ConcurrentChange = new(
        "KnowledgeDocument.ConcurrentChange",
        "The document was changed by another operation. Refresh and try again.");
}
