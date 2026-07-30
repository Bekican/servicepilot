using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Authentication;
using ServicePilot.Domain.Users;

namespace ServicePilot.Infrastructure.Authentication;

internal sealed class JwtAccessTokenProvider(
    JwtOptions options,
    TimeProvider timeProvider)
    : IAccessTokenProvider
{
    public AccessToken Create(User user)
    {
        DateTimeOffset issuedAtUtc = timeProvider.GetUtcNow();
        DateTimeOffset expiresAtUtc =
            issuedAtUtc.AddMinutes(
                options.AccessTokenLifetimeMinutes);

        Claim[] claims =
        [
            new(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),
            new(
                JwtRegisteredClaimNames.Email,
                user.Email),
            new(
                AuthenticationClaimNames.OrganizationId,
                user.OrganizationId.ToString()),
            new(
                AuthenticationClaimNames.Role,
                user.Role)
        ];

        SymmetricSecurityKey securityKey = new(
            Encoding.UTF8.GetBytes(options.SigningKey));

        SigningCredentials signingCredentials = new(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            options.Issuer,
            options.Audience,
            claims,
            issuedAtUtc.UtcDateTime,
            expiresAtUtc.UtcDateTime,
            signingCredentials);

        string tokenValue =
            new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessToken(
            tokenValue,
            expiresAtUtc);
    }
}