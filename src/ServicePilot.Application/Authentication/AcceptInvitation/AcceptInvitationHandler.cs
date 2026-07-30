using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Abstractions.Persistence.Exceptions;
using ServicePilot.Application.Common;
using ServicePilot.Application.Users;
using ServicePilot.Application.Users.Invitations;
using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Authentication.AcceptInvitation;

public sealed class AcceptInvitationHandler(
    IUserInvitationRepository invitationRepository,
    IUserRepository userRepository,
    IInvitationTokenService tokenService,
    IPasswordHasher passwordHasher,
    IAccessTokenProvider accessTokenProvider,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<AuthenticationResponse>> HandleAsync(
        AcceptInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        string token = command.Token?.Trim() ?? string.Empty;
        string firstName =
            command.FirstName?.Trim() ?? string.Empty;
        string lastName =
            command.LastName?.Trim() ?? string.Empty;
        string password = command.Password ?? string.Empty;

        Error? validationError =
            Validate(token, firstName, lastName, password);

        if (validationError is not null)
        {
            return Result<AuthenticationResponse>.Failure(
                validationError);
        }

        string tokenHash = tokenService.Hash(token);

        UserInvitation? invitation =
            await invitationRepository.GetByTokenHashAsync(
                tokenHash,
                cancellationToken);
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();

        if (invitation is null
            || !invitation.CanBeAcceptedAt(nowUtc))
        {
            return Result<AuthenticationResponse>.Failure(
                InvitationErrors.InvalidOrExpired);
        }

        User? existingUser =
            await userRepository.GetByOrganizationAndEmailAsync(
                invitation.OrganizationId,
                invitation.Email,
                cancellationToken);

        if (existingUser is not null)
        {
            return Result<AuthenticationResponse>.Failure(
                InvitationErrors.UserAlreadyExists);
        }

        User user = new(
            Guid.NewGuid(),
            invitation.OrganizationId,
            firstName,
            lastName,
            invitation.Email,
            passwordHasher.Hash(password),
            invitation.Role,
            nowUtc);

        invitation.MarkAccepted(nowUtc);
        userRepository.Add(user);

        try
        {
            await unitOfWork.SaveChangesAsync(
                cancellationToken);
        }
        catch (UniqueConstraintViolationException exception)
            when (
                exception.ConstraintName
                == "ux_users_organization_id_email")
        {
            return Result<AuthenticationResponse>.Failure(
                InvitationErrors.UserAlreadyExists);
        }

        AccessToken accessToken =
            accessTokenProvider.Create(user);

        return Result<AuthenticationResponse>.Success(
            new AuthenticationResponse(
                user.Id,
                user.OrganizationId,
                user.Role,
                accessToken.Value,
                accessToken.ExpiresAtUtc));
    }

    private static Error? Validate(
        string token,
        string firstName,
        string lastName,
        string password)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return InvitationErrors.TokenIsRequired;
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            return AuthenticationErrors.FirstNameIsRequired;
        }

        if (firstName.Length > User.MaxFirstNameLength)
        {
            return AuthenticationErrors.FirstNameTooLong;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return AuthenticationErrors.LastNameIsRequired;
        }

        if (lastName.Length > User.MaxLastNameLength)
        {
            return AuthenticationErrors.LastNameTooLong;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return AuthenticationErrors.PasswordIsRequired;
        }

        if (password.Length < AuthenticationRules.MinPasswordLength)
        {
            return AuthenticationErrors.PasswordTooShort;
        }

        if (password.Length > AuthenticationRules.MaxPasswordLength)
        {
            return AuthenticationErrors.PasswordTooLong;
        }

        return null;
    }
}