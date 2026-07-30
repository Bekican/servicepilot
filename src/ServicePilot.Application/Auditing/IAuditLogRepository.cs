using ServicePilot.Domain.Auditing;

namespace ServicePilot.Application.Auditing;

public interface IAuditLogRepository
{
    void Add(AuditLog auditLog);
}