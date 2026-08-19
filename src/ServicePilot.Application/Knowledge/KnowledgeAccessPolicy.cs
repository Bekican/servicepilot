using ServicePilot.Domain.Knowledge;
using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Knowledge;

public static class KnowledgeAccessPolicy
{
    public static IReadOnlyCollection<KnowledgeDocumentAccessScope>
        AllowedScopes(string role) =>
        role switch
        {
            UserRoles.Owner or UserRoles.Admin =>
                Enum.GetValues<KnowledgeDocumentAccessScope>(),
            UserRoles.Dispatcher =>
            [
                KnowledgeDocumentAccessScope.Shared,
                KnowledgeDocumentAccessScope.Operations
            ],
            UserRoles.Technician =>
            [
                KnowledgeDocumentAccessScope.Shared
            ],
            _ => []
        };
}
