namespace ServicePilot.Application.Authentication.AcceptInvitation;

public sealed record AcceptInvitationCommand(
    string Token,
    string FirstName,
    string LastName,
    string Password);