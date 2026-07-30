namespace ServicePilot.Application.Abstractions.Authentication;

public interface IInvitationTokenService
{
    InvitationToken Create();

    string Hash(string rawToken);
}