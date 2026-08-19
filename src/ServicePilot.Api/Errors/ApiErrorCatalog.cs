using ServicePilot.Application.Appointments;
using ServicePilot.Application.Authentication;
using ServicePilot.Application.Authentication.PasswordReset;
using ServicePilot.Application.Common;
using ServicePilot.Application.Customers;
using ServicePilot.Application.Knowledge;
using ServicePilot.Application.Organizations;
using ServicePilot.Application.Reminders;
using ServicePilot.Application.Services;
using ServicePilot.Application.Users.Invitations;
using ServicePilot.Application.Users.Management;

namespace ServicePilot.Api.Errors;

public static class ApiErrorCatalog
{
    private static readonly IReadOnlyDictionary<
        string,
        ApiProblemDescriptor> Descriptors = Build();

    public static bool TryResolve(
        Error error,
        out ApiProblemDescriptor descriptor) =>
        Descriptors.TryGetValue(
            error.Code,
            out descriptor!);

    private static IReadOnlyDictionary<
        string,
        ApiProblemDescriptor> Build()
    {
        Dictionary<string, ApiProblemDescriptor> descriptors =
            new(StringComparer.Ordinal);

        Add(
            descriptors,
            StatusCodes.Status400BadRequest,
            AuthenticationErrors.OrganizationNameIsRequired,
            AuthenticationErrors.OrganizationNameTooLong,
            AuthenticationErrors.OrganizationSlugIsRequired,
            AuthenticationErrors.OrganizationSlugTooLong,
            AuthenticationErrors.InvalidOrganizationSlug,
            AuthenticationErrors.InvalidTimeZone,
            AuthenticationErrors.FirstNameIsRequired,
            AuthenticationErrors.FirstNameTooLong,
            AuthenticationErrors.LastNameIsRequired,
            AuthenticationErrors.LastNameTooLong,
            AuthenticationErrors.EmailIsRequired,
            AuthenticationErrors.InvalidEmail,
            AuthenticationErrors.EmailTooLong);
        Add(
            descriptors,
            StatusCodes.Status400BadRequest,
            "Password does not meet requirements.",
            AuthenticationErrors.PasswordIsRequired,
            AuthenticationErrors.PasswordTooShort,
            AuthenticationErrors.PasswordTooLong);
        Add(
            descriptors,
            StatusCodes.Status401Unauthorized,
            AuthenticationErrors.InvalidCredentials);
        Add(
            descriptors,
            StatusCodes.Status400BadRequest,
            PasswordResetErrors.InvalidOrExpired);
        Add(
            descriptors,
            StatusCodes.Status409Conflict,
            AuthenticationErrors.OrganizationSlugAlreadyExists,
            AuthenticationErrors.EmailAlreadyExists);

        Add(
            descriptors,
            StatusCodes.Status404NotFound,
            CustomerErrors.NotFound,
            CustomerErrors.AddressNotFound);
        Add(
            descriptors,
            StatusCodes.Status409Conflict,
            CustomerErrors.EmailAlreadyExists,
            CustomerErrors.EmailBelongsToInactiveCustomer,
            CustomerErrors.PhoneAlreadyExists,
            CustomerErrors.PhoneBelongsToInactiveCustomer,
            CustomerErrors.PrimaryAddressConflict);
        Add(
            descriptors,
            StatusCodes.Status400BadRequest,
            CustomerErrors.InvalidData);
        Add(
            descriptors,
            StatusCodes.Status400BadRequest,
            CustomerErrors.FirstNameRequired,
            CustomerErrors.LastNameRequired,
            CustomerErrors.CompanyNameRequired,
            CustomerErrors.InvalidType,
            CustomerErrors.FirstNameTooLong,
            CustomerErrors.LastNameTooLong,
            CustomerErrors.CompanyNameTooLong,
            CustomerErrors.ContactPersonTooLong,
            CustomerErrors.EmailTooLong,
            CustomerErrors.InvalidEmail,
            CustomerErrors.InvalidPhone);

        Add(
            descriptors,
            StatusCodes.Status404NotFound,
            ServiceCatalogErrors.NotFound);
        Add(
            descriptors,
            StatusCodes.Status409Conflict,
            ServiceCatalogErrors.NameAlreadyExists,
            ServiceCatalogErrors.NameBelongsToInactiveService);
        Add(
            descriptors,
            StatusCodes.Status400BadRequest,
            ServiceCatalogErrors.InvalidData,
            ServiceCatalogErrors.InvalidName,
            ServiceCatalogErrors.InvalidDuration);

        Add(
            descriptors,
            StatusCodes.Status404NotFound,
            AppointmentErrors.NotFound);
        Add(
            descriptors,
            StatusCodes.Status403Forbidden,
            AppointmentErrors.Forbidden);
        Add(
            descriptors,
            StatusCodes.Status409Conflict,
            AppointmentErrors.TechnicianOverlap);
        Add(
            descriptors,
            StatusCodes.Status400BadRequest,
            AppointmentErrors.InvalidData,
            AppointmentErrors.CustomerUnavailable,
            AppointmentErrors.ServiceUnavailable,
            AppointmentErrors.TechnicianUnavailable,
            AppointmentErrors.InvalidTransition);

        Add(
            descriptors,
            StatusCodes.Status404NotFound,
            ReminderErrors.NotFound);
        Add(
            descriptors,
            StatusCodes.Status409Conflict,
            ReminderErrors.InvalidRetry);

        Add(
            descriptors,
            StatusCodes.Status404NotFound,
            KnowledgeDocumentErrors.NotFound);
        Add(
            descriptors,
            StatusCodes.Status409Conflict,
            KnowledgeDocumentErrors.DuplicateContent,
            KnowledgeDocumentErrors.InvalidRetry);
        Add(
            descriptors,
            StatusCodes.Status400BadRequest,
            KnowledgeDocumentErrors.InvalidPdf,
            KnowledgeDocumentErrors.FileTooLarge,
            KnowledgeDocumentErrors.InvalidFileName,
            KnowledgeDocumentErrors.InvalidType,
            KnowledgeDocumentErrors.InvalidAccessScope);
        Add(
            descriptors,
            StatusCodes.Status400BadRequest,
            KnowledgeAssistantErrors.InvalidQuestion);
        Add(
            descriptors,
            StatusCodes.Status503ServiceUnavailable,
            KnowledgeAssistantErrors.ProviderUnavailable);

        Add(
            descriptors,
            StatusCodes.Status404NotFound,
            InvitationErrors.NotFound);
        Add(
            descriptors,
            StatusCodes.Status409Conflict,
            InvitationErrors.UserAlreadyExists,
            InvitationErrors.NotPending,
            InvitationErrors.ConcurrentRequest);
        Add(
            descriptors,
            StatusCodes.Status503ServiceUnavailable,
            InvitationErrors.EmailDeliveryFailed);
        Add(
            descriptors,
            StatusCodes.Status400BadRequest,
            InvitationErrors.EmailIsRequired,
            InvitationErrors.InvalidEmail,
            InvitationErrors.EmailTooLong,
            InvitationErrors.InvalidRole,
            InvitationErrors.InvalidOrExpired,
            InvitationErrors.TokenIsRequired);

        Add(
            descriptors,
            StatusCodes.Status404NotFound,
            UserManagementErrors.NotFound);
        Add(
            descriptors,
            StatusCodes.Status409Conflict,
            UserManagementErrors.SelfModificationNotAllowed,
            UserManagementErrors.LastActiveOwner);
        Add(
            descriptors,
            StatusCodes.Status400BadRequest,
            UserManagementErrors.InvalidRole);

        Add(
            descriptors,
            StatusCodes.Status409Conflict,
            OrganizationErrors.SlugAlreadyExists);
        Add(
            descriptors,
            StatusCodes.Status400BadRequest,
            OrganizationErrors.NameIsRequired,
            OrganizationErrors.NameTooLong,
            OrganizationErrors.SlugIsRequired,
            OrganizationErrors.SlugTooLong,
            OrganizationErrors.InvalidSlug);

        return descriptors;
    }

    private static void Add(
        IDictionary<string, ApiProblemDescriptor> descriptors,
        int statusCode,
        params Error[] errors)
    {
        foreach (Error error in errors)
        {
            descriptors.Add(
                error.Code,
                new ApiProblemDescriptor(
                    error.Code,
                    statusCode,
                    error.Message));
        }
    }

    private static void Add(
        IDictionary<string, ApiProblemDescriptor> descriptors,
        int statusCode,
        string safeDetail,
        params Error[] errors)
    {
        foreach (Error error in errors)
        {
            descriptors.Add(
                error.Code,
                new ApiProblemDescriptor(
                    error.Code,
                    statusCode,
                    safeDetail));
        }
    }
}
