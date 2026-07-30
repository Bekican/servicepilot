using ServicePilot.Application.Auditing;
using ServicePilot.Domain.Auditing;

namespace ServicePilot.UnitTests.Authentication;

internal sealed class FakeAuditLogRepository
    : IAuditLogRepository
{
    private readonly List<AuditLog> _auditLogs = [];

    public IReadOnlyCollection<AuditLog> AuditLogs =>
        _auditLogs.AsReadOnly();

    public void Add(AuditLog auditLog)
    {
        _auditLogs.Add(auditLog);
    }
}