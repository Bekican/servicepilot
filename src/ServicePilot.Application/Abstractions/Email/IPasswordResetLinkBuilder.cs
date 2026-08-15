namespace ServicePilot.Application.Abstractions.Email;

public interface IPasswordResetLinkBuilder
{
    string Build(string rawToken);
}