using ServicePilot.Application.Common;

namespace ServicePilot.Application.Knowledge;

public static class KnowledgeAssistantErrors
{
    public static readonly Error InvalidQuestion = Error.ForField(
        "KnowledgeAssistant.InvalidQuestion",
        "Question must be between 1 and 2000 characters.",
        "Question",
        "InvalidQuestion");

    public static readonly Error ProviderUnavailable = new(
        "KnowledgeAssistant.ProviderUnavailable",
        "The local knowledge assistant is unavailable.");
}
