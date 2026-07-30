using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Users.Management;

public sealed record UserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAtUtc)
{
    public static UserResponse FromUser(User user)
    {
        return new UserResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Role,
            user.IsActive,
            user.CreatedAtUtc);
    }
}