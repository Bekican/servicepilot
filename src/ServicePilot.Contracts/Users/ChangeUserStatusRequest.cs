namespace ServicePilot.Contracts.Users;

public sealed record ChangeUserStatusRequest(
    bool IsActive);