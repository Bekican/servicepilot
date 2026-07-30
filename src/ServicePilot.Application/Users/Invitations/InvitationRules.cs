using ServicePilot.Application.Abstractions.Email;
using ServicePilot.Application.Authentication;
using ServicePilot.Application.Common;
using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Users.Invitations;

internal static class InvitationRules
{
    public static Error? Validate(string email, string role)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return InvitationErrors.EmailIsRequired;
        }

        if (email.Length > UserInvitation.MaxEmailLength)
        {
            return InvitationErrors.EmailTooLong;
        }

        if (!AuthenticationRules.IsValidEmail(email))
        {
            return InvitationErrors.InvalidEmail;
        }

        if (!UserRoles.IsSupported(role))
        {
            return InvitationErrors.InvalidRole;
        }

        return null;
    }

    public static EmailMessage CreateEmail(
        string recipient,
        string invitationLink,
        DateTimeOffset expiresAtUtc)
    {
        return new EmailMessage(
            recipient,
            "Your ServicePilot invitation",
            $"Use this link to create your account: {invitationLink}{Environment.NewLine}"
            + $"The link expires at {expiresAtUtc:O}.");
    }
}