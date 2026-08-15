using ServicePilot.Application.Common;

namespace ServicePilot.Application.Authentication;

public static class AuthenticationErrors
{
    public static readonly Error OrganizationNameIsRequired = Error.ForField(
        "Authentication.OrganizationNameIsRequired",
        "Organization name is required", "organizationName", "Required");

    public static readonly Error OrganizationNameTooLong = Error.ForField(
        "Authentication.OrganizationNameTooLong",
        "Organization name is too long", "organizationName", "TooLong");

    public static readonly Error OrganizationSlugIsRequired = Error.ForField(
        "Authentication.OrganizationSlugIsRequired",
        "Organization slug is required", "organizationSlug", "Required");

    public static readonly Error OrganizationSlugTooLong = Error.ForField(
        "Authentication.OrganizationSlugTooLong",
        "Organization slug is too long", "organizationSlug", "TooLong");

    public static readonly Error InvalidOrganizationSlug = Error.ForField(
        "Authentication.InvalidOrganizationSlug",
        "Organization slug contains invalid characters", "organizationSlug", "InvalidSlug");

    public static readonly Error OrganizationSlugAlreadyExists = new(
        "Authentication.OrganizationSlugAlreadyExists",
        "An organization with this slug already exists");

    public static readonly Error InvalidTimeZone = Error.ForField(
        "Authentication.InvalidTimeZone",
        "Organization time zone is invalid", "timeZoneId", "Invalid");

    public static readonly Error FirstNameIsRequired = Error.ForField(
        "Authentication.FirstNameIsRequired",
        "First name is required", "firstName", "Required");

    public static readonly Error FirstNameTooLong = Error.ForField(
        "Authentication.FirstNameTooLong",
        "First name is too long", "firstName", "TooLong");

    public static readonly Error LastNameIsRequired = Error.ForField(
        "Authentication.LastNameIsRequired",
        "Last name is required", "lastName", "Required");

    public static readonly Error LastNameTooLong = Error.ForField(
        "Authentication.LastNameTooLong",
        "Last name is too long", "lastName", "TooLong");

    public static readonly Error EmailIsRequired = Error.ForField(
        "Authentication.EmailIsRequired",
        "Email is required", "email", "Required");

    public static readonly Error InvalidEmail = Error.ForField(
        "Authentication.InvalidEmail",
        "Email is invalid", "email", "InvalidEmail");

    public static readonly Error EmailTooLong = Error.ForField(
        "Authentication.EmailTooLong",
        "Email is too long", "email", "TooLong");

    public static readonly Error EmailAlreadyExists = new(
        "Authentication.EmailAlreadyExists",
        "A user with this email already exists in the organization");

    public static readonly Error PasswordIsRequired = Error.ForField(
        "Authentication.PasswordIsRequired",
        "Password is required", "password", "Required");

    public static readonly Error PasswordTooShort = Error.ForField(
        "Authentication.PasswordTooShort",
        "Password must contain at least 8 characters", "password", "TooShort");

    public static readonly Error PasswordTooLong = Error.ForField(
        "Authentication.PasswordTooLong",
        "Password cannot exceed 128 characters", "password", "TooLong");

    public static readonly Error InvalidCredentials = new(
        "Authentication.InvalidCredentials",
        "Organization, email or password is invalid");
}