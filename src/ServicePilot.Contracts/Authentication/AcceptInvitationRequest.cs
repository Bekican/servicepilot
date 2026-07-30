namespace ServicePilot.Contracts.Authentication;

public sealed record AcceptInvitationRequest(
    string Token,
    string FirstName,
    string LastName,
    string Password);