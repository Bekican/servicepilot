using ServicePilot.Application.Common;

namespace ServicePilot.Application.Authentication;

public static class AuthenticationErrors
{
    public static readonly Error OrganizationNameIsRequired = new(
        "Authentication.OrganizationNameIsRequired",
        "Organization name is required");

    public static readonly Error OrganizationNameTooLong = new(
        "Authentication.OrganizationNameTooLong",
        "Organization name is too long");

    public static readonly Error OrganizationSlugIsRequired = new(
        "Authentication.OrganizationSlugIsRequired",
        "Organization slug is required");

    public static readonly Error OrganizationSlugTooLong = new(
        "Authentication.OrganizationSlugTooLong",
        "Organization slug is too long");

    public static readonly Error InvalidOrganizationSlug = new(
        "Authentication.InvalidOrganizationSlug",
        "Organization slug contains invalid characters");

    public static readonly Error OrganizationSlugAlreadyExists = new(
        "Authentication.OrganizationSlugAlreadyExists",
        "An organization with this slug already exists");

    public static readonly Error InvalidTimeZone = new(
        "Authentication.InvalidTimeZone",
        "Organization time zone is invalid");

    public static readonly Error FirstNameIsRequired = new(
        "Authentication.FirstNameIsRequired",
        "First name is required");

    public static readonly Error FirstNameTooLong = new(
        "Authentication.FirstNameTooLong",
        "First name is too long");

    public static readonly Error LastNameIsRequired = new(
        "Authentication.LastNameIsRequired",
        "Last name is required");

    public static readonly Error LastNameTooLong = new(
        "Authentication.LastNameTooLong",
        "Last name is too long");

    public static readonly Error EmailIsRequired = new(
        "Authentication.EmailIsRequired",
        "Email is required");

    public static readonly Error InvalidEmail = new(
        "Authentication.InvalidEmail",
        "Email is invalid");

    public static readonly Error EmailTooLong = new(
        "Authentication.EmailTooLong",
        "Email is too long");

    public static readonly Error EmailAlreadyExists = new(
        "Authentication.EmailAlreadyExists",
        "A user with this email already exists in the organization");

    public static readonly Error PasswordIsRequired = new(
        "Authentication.PasswordIsRequired",
        "Password is required");

    public static readonly Error PasswordTooShort = new(
        "Authentication.PasswordTooShort",
        "Password must contain at least 8 characters");

    public static readonly Error PasswordTooLong = new(
        "Authentication.PasswordTooLong",
        "Password cannot exceed 128 characters");

    public static readonly Error InvalidCredentials = new(
        "Authentication.InvalidCredentials",
        "Organization, email or password is invalid");
}