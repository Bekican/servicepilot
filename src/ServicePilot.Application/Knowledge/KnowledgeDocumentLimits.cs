namespace ServicePilot.Application.Knowledge;

public sealed record KnowledgeDocumentLimits(
    int MaximumDocumentsPerOrganization,
    long MaximumStorageBytesPerOrganization)
{
    public static readonly KnowledgeDocumentLimits Default = new(
        500,
        5L * 1024 * 1024 * 1024);
}
