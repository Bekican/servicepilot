namespace ServicePilot.Application.Users.Management.ChangeUserRole;

public sealed record ChangeUserRoleCommand(
    Guid UserId,
    string Role);