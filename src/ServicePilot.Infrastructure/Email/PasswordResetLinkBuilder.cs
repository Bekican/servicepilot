using ServicePilot.Application.Abstractions.Email;

namespace ServicePilot.Infrastructure.Email;

internal sealed class PasswordResetLinkBuilder(PasswordResetLinkOptions options)
    : IPasswordResetLinkBuilder
{
    public string Build(string rawToken)
    {
        string baseUrl = options.PublicBaseUrl.EndsWith("/", StringComparison.Ordinal)
            ? options.PublicBaseUrl
            : options.PublicBaseUrl + "/";
        Uri resetUri = new(new Uri(baseUrl), "password-reset");
        return $"{resetUri}?token={Uri.EscapeDataString(rawToken)}";
    }
}
