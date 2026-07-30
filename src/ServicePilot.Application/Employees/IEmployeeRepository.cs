using ServicePilot.Domain.Employees;

namespace ServicePilot.Application.Employees;

public interface IEmployeeRepository
{
    Task<bool> EmailExistsAsync(
        Guid organizationId,
        string email,
        CancellationToken cancellationToken = default);
    void Add(Employee employee);
}