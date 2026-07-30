using ServicePilot.Application.Abstractions.Email;

namespace ServicePilot.Infrastructure.Email;

internal sealed class InvitationLinkBuilder(
    InvitationLinkOptions options)
    : IInvitationLinkBuilder
{
    public string Build(string rawToken)
    {
        char separator =
            options.PublicBaseUrl.Contains(
                '?',
                StringComparison.Ordinal)
                ? '&'
                : '?';

        return $"{options.PublicBaseUrl}{separator}token="
            + Uri.EscapeDataString(rawToken);
    }
}