namespace ServicePilot.Contracts.Users;

public sealed record TechnicianResponse(
    Guid Id,
    string FirstName,
    string LastName);