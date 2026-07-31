using ServicePilot.Application.Abstractions.Authentication;

using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Users.Technicians;

public sealed class TechnicianDirectoryService(
    ICurrentUserContext currentUser,
    IUserRepository userRepository)
{
    public async Task<IReadOnlyList<TechnicianResponse>>
        ListActiveAsync(
            CancellationToken cancellationToken = default)
    {
        IReadOnlyList<User> users =
            await userRepository.ListAsync(
                currentUser.OrganizationId,
                cancellationToken);

        return users
            .Where(user =>
                user.IsActive
                && user.Role == UserRoles.Technician)
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .Select(user => new TechnicianResponse(
                user.Id,
                user.FirstName,
                user.LastName))
            .ToArray();
    }
}

public sealed record TechnicianResponse(
    Guid Id,
    string FirstName,
    string LastName);