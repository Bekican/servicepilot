namespace ServicePilot.Application.Abstractions.Authentication;

public interface ISessionVersionValidator
{
    Task<bool> IsValidAsync(
        Guid organizationId,
        Guid userId,
        int sessionVersion,
        CancellationToken cancellationToken = default);
}
