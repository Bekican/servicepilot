using Microsoft.Extensions.Configuration;

namespace ServicePilot.Infrastructure.Email;

public sealed record PasswordResetLinkOptions(string PublicBaseUrl)
{
    public const string SectionName = "PasswordReset";

    public static PasswordResetLinkOptions FromConfiguration(IConfiguration configuration)
    {
        string publicBaseUrl = configuration[$"{SectionName}:PublicBaseUrl"]
            ?? throw new InvalidOperationException(
                "Password reset public base URL is not configured");
        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException(
                "Password reset public base URL must be absolute");
        return new PasswordResetLinkOptions(publicBaseUrl);
    }
}
