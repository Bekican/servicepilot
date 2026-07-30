namespace ServicePilot.Application.Users.Management.ChangeUserStatus;

public sealed record ChangeUserStatusCommand(
    Guid UserId,
    bool IsActive);