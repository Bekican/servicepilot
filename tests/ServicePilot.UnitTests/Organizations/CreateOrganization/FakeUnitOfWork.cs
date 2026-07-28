using ServicePilot.Application.Abstractions.Persistence;

namespace ServicePilot.UnitTests.Organizations.CreateOrganization;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;

        return Task.FromResult(1);
    }
}