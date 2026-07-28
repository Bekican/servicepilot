using System.Text.RegularExpressions;

namespace ServicePilot.Application.Organizations.CreateOrganization;


internal static partial class OrganizationSlug
{
    [GeneratedRegex(
        "^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ValidSlugRegex();

    public static string Normalize(string slug)
    {
        return slug
            .Trim()
            .ToLowerInvariant();
    }

    public static bool IsValid(string slug)
    {
        return ValidSlugRegex().IsMatch(slug);
    }
}