using Microsoft.Extensions.Configuration;

namespace ServicePilot.Infrastructure.Authentication;

public sealed record JwtOptions(
    string Issuer,
    string Audience,
    string SigningKey,
    int AccessTokenLifetimeMinutes)
{
    public const string SectionName = "Jwt";

    public static JwtOptions FromConfiguration(
        IConfiguration configuration)
    {
        string issuer =
            configuration[$"{SectionName}:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer is not configured");

        string audience =
            configuration[$"{SectionName}:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience is not configured");

        string signingKey =
            configuration[$"{SectionName}:SigningKey"]
            ?? throw new InvalidOperationException(
                "JWT signing key is not configured");

        if (signingKey.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at least 32 characters");
        }

        int accessTokenLifetimeMinutes =
            configuration.GetValue<int>(
                $"{SectionName}:AccessTokenLifetimeMinutes");

        if (accessTokenLifetimeMinutes <= 0)
        {
            throw new InvalidOperationException(
                "JWT access token lifetime must be positive");
        }

        return new JwtOptions(
            issuer,
            audience,
            signingKey,
            accessTokenLifetimeMinutes);
    }
}