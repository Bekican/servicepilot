using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Common;
using ServicePilot.Application.Organizations;
using ServicePilot.Application.Organizations.CreateOrganization;
using ServicePilot.Application.Users;
using ServicePilot.Domain.Organizations;
using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Authentication.Login;

public sealed class LoginHandler(
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IAccessTokenProvider accessTokenProvider)
{
    public async Task<Result<AuthenticationResponse>> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        string organizationSlug =
            OrganizationSlug.Normalize(
                command.OrganizationSlug ?? string.Empty);
        string email =
            AuthenticationRules.NormalizeEmail(
                command.Email ?? string.Empty);
        string password = command.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(organizationSlug)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password))
        {
            return Result<AuthenticationResponse>.Failure(
                AuthenticationErrors.InvalidCredentials);
        }

        Organization? organization =
            await organizationRepository.GetBySlugAsync(
                organizationSlug,
                cancellationToken);

        if (organization is null)
        {
            return Result<AuthenticationResponse>.Failure(
                AuthenticationErrors.InvalidCredentials);
        }

        User? user =
            await userRepository.GetByOrganizationAndEmailAsync(
                organization.Id,
                email,
                cancellationToken);

        if (user is null
            || !user.IsActive
            || !passwordHasher.Verify(
                user.PasswordHash,
                password))
        {
            return Result<AuthenticationResponse>.Failure(
                AuthenticationErrors.InvalidCredentials);
        }

        AccessToken accessToken = accessTokenProvider.Create(user);

        AuthenticationResponse response = new(
            user.Id,
            user.OrganizationId,
            user.Role,
            accessToken.Value,
            accessToken.ExpiresAtUtc);

        return Result<AuthenticationResponse>.Success(response);
    }
}