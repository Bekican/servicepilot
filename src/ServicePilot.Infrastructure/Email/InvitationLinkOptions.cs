using Microsoft.Extensions.Configuration;

namespace ServicePilot.Infrastructure.Email;

public sealed record InvitationLinkOptions(
    string PublicBaseUrl)
{
    public const string SectionName = "Invitations";

    public static InvitationLinkOptions FromConfiguration(
        IConfiguration configuration)
    {
        string publicBaseUrl =
            configuration[
                $"{SectionName}:PublicBaseUrl"]
            ?? throw new InvalidOperationException(
                "Invitation public base URL is not configured");

        if (!Uri.TryCreate(
            publicBaseUrl,
            UriKind.Absolute,
            out _))
        {
            throw new InvalidOperationException(
                "Invitation public base URL must be absolute");
        }

        return new InvitationLinkOptions(publicBaseUrl);
    }
}