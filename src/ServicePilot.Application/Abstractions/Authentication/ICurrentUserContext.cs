namespace ServicePilot.Application.Abstractions.Authentication;

public interface ICurrentUserContext
{
    Guid UserId { get; }
    Guid OrganizationId { get; }
    string Role { get; }
}