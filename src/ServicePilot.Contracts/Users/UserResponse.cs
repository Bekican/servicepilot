namespace ServicePilot.Contracts.Users;

public sealed record UserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);