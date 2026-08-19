namespace ServicePilot.Domain.Knowledge;

public enum KnowledgeDocumentStatus
{
    Pending,
    Processing,
    Ready,
    Failed,
    Deleting,
    Deleted
}
