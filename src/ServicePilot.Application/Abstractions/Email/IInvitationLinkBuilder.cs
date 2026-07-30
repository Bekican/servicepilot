namespace ServicePilot.Application.Abstractions.Email;

public interface IInvitationLinkBuilder
{
    string Build(string rawToken);
}