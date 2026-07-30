using ServicePilot.Application.Auditing;
using ServicePilot.Domain.Auditing;
using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.Infrastructure.Auditing;

internal sealed class AuditLogRepository(
    ServicePilotDbContext dbContext)
    : IAuditLogRepository
{
    public void Add(AuditLog auditLog)
    {
        dbContext.AuditLogs.Add(auditLog);
    }
}