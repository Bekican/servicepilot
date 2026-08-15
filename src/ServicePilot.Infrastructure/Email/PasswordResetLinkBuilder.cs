using ServicePilot.Application.Abstractions.Email;

namespace ServicePilot.Infrastructure.Email;

internal sealed class PasswordResetLinkBuilder(InvitationLinkOptions options)
    : IPasswordResetLinkBuilder
{
    public string Build(string rawToken)
    {
        Uri invitationUri = new(options.PublicBaseUrl);
        Uri resetUri = new(invitationUri, "/password-reset");
        return $"{resetUri}?token={Uri.EscapeDataString(rawToken)}";
    }
}