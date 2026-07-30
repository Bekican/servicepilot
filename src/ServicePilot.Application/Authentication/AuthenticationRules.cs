using System.Net.Mail;

namespace ServicePilot.Application.Authentication;

internal static class AuthenticationRules
{
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    public static bool IsValidEmail(string email)
    {
        return MailAddress.TryCreate(
            email,
            out MailAddress? mailAddress)
            && mailAddress.Address == email;
    }
}