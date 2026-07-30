using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Abstractions.Persistence.Exceptions;
using ServicePilot.Application.Common;
using ServicePilot.Application.Organizations;
using ServicePilot.Application.Organizations.CreateOrganization;
using ServicePilot.Application.Users;
using ServicePilot.Domain.Organizations;
using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Authentication.Register;

public sealed class RegisterOrganizationOwnerHandler(
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IAccessTokenProvider accessTokenProvider,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<AuthenticationResponse>> HandleAsync(
        RegisterOrganizationOwnerCommand command,
        CancellationToken cancellationToken = default)
    {
        string organizationName =
            command.OrganizationName?.Trim() ?? string.Empty;
        string organizationSlug =
            OrganizationSlug.Normalize(
                command.OrganizationSlug ?? string.Empty);
        string firstName =
            command.FirstName?.Trim() ?? string.Empty;
        string lastName =
            command.LastName?.Trim() ?? string.Empty;
        string email =
            AuthenticationRules.NormalizeEmail(
                command.Email ?? string.Empty);
        string password = command.Password ?? string.Empty;

        Error? validationError = Validate(
            organizationName,
            organizationSlug,
            firstName,
            lastName,
            email,
            password);

        if (validationError is not null)
        {
            return Result<AuthenticationResponse>.Failure(
                validationError);
        }

        bool slugExists =
            await organizationRepository.SlugExistsAsync(
                organizationSlug,
                cancellationToken);

        if (slugExists)
        {
            return Result<AuthenticationResponse>.Failure(
                AuthenticationErrors.OrganizationSlugAlreadyExists);
        }

        DateTimeOffset createdAtUtc = timeProvider.GetUtcNow();

        Organization organization = new(
            Guid.NewGuid(),
            organizationName,
            organizationSlug,
            createdAtUtc);

        string passwordHash = passwordHasher.Hash(password);

        User owner = new(
            Guid.NewGuid(),
            organization.Id,
            firstName,
            lastName,
            email,
            passwordHash,
            UserRoles.Owner,
            createdAtUtc);

        organizationRepository.Add(organization);
        userRepository.Add(owner);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException exception)
            when (exception.ConstraintName == "ux_organizations_slug")
        {
            return Result<AuthenticationResponse>.Failure(
                AuthenticationErrors.OrganizationSlugAlreadyExists);
        }
        catch (UniqueConstraintViolationException exception)
            when (
                exception.ConstraintName
                == "ux_users_organization_id_email")
        {
            return Result<AuthenticationResponse>.Failure(
                AuthenticationErrors.EmailAlreadyExists);
        }

        AccessToken accessToken = accessTokenProvider.Create(owner);

        AuthenticationResponse response = new(
            owner.Id,
            owner.OrganizationId,
            owner.Role,
            accessToken.Value,
            accessToken.ExpiresAtUtc);

        return Result<AuthenticationResponse>.Success(response);
    }

    private static Error? Validate(
        string organizationName,
        string organizationSlug,
        string firstName,
        string lastName,
        string email,
        string password)
    {
        if (string.IsNullOrWhiteSpace(organizationName))
        {
            return AuthenticationErrors.OrganizationNameIsRequired;
        }

        if (organizationName.Length > Organization.MaxNameLength)
        {
            return AuthenticationErrors.OrganizationNameTooLong;
        }

        if (string.IsNullOrWhiteSpace(organizationSlug))
        {
            return AuthenticationErrors.OrganizationSlugIsRequired;
        }

        if (organizationSlug.Length > Organization.MaxSlugLength)
        {
            return AuthenticationErrors.OrganizationSlugTooLong;
        }

        if (!OrganizationSlug.IsValid(organizationSlug))
        {
            return AuthenticationErrors.InvalidOrganizationSlug;
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

        if (string.IsNullOrWhiteSpace(email))
        {
            return AuthenticationErrors.EmailIsRequired;
        }

        if (email.Length > User.MaxEmailLength)
        {
            return AuthenticationErrors.EmailTooLong;
        }

        if (!AuthenticationRules.IsValidEmail(email))
        {
            return AuthenticationErrors.InvalidEmail;
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