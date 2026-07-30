using ServicePilot.Application.Common;

namespace ServicePilot.Application.Organizations;

public static class OrganizationErrors
{
    public static readonly Error NameIsRequired = new(
        "Organizations.NameIsRequired",
        "Organization name is required");

    public static readonly Error SlugIsRequired = new(
        "Organizations.SlugRequired",
        "Organization slug is required");

    public static readonly Error InvalidSlug = new(
        "Organizations.InvalidSlug",
        "Organization slug contains invalid characters");

    public static readonly Error SlugAlreadyExists = new(
        "Organizations.SlugAlreadyExists",
        "An organization with this slug already exists.");
}