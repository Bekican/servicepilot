using System.Net.Mail;
using System.Text.RegularExpressions;

namespace ServicePilot.Domain.Customers;

public static partial class CustomerContactNormalizer
{
    public static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        string normalized = email.Trim().ToLowerInvariant();

        if (!MailAddress.TryCreate(
            normalized,
            out MailAddress? mailAddress)
            || mailAddress.Address != normalized)
        {
            throw new ArgumentException(
                "Customer email is invalid",
                nameof(email));
        }

        return normalized;
    }

    public static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        string normalized = phone
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("(", string.Empty)
            .Replace(")", string.Empty);

        if (LocalTurkeyPhoneRegex().IsMatch(normalized))
        {
            normalized = "+90" + normalized[1..];
        }

        if (!E164Regex().IsMatch(normalized))
        {
            throw new ArgumentException(
                "Customer phone must be a valid E.164 number",
                nameof(phone));
        }

        return normalized;
    }

    [GeneratedRegex(
        @"^\+[1-9]\d{7,14}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex E164Regex();

    [GeneratedRegex(
        @"^05\d{9}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex LocalTurkeyPhoneRegex();
}