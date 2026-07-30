using ServicePilot.Application.Common;

namespace ServicePilot.Application.Services;

public static class ServiceCatalogErrors
{
    public static readonly Error NotFound = new(
        "Service.NotFound",
        "Service was not found");
    public static readonly Error InvalidData = new(
        "Service.InvalidData",
        "Service data is invalid");
    public static readonly Error NameAlreadyExists = new(
        "Service.NameAlreadyExists",
        "A service with this name already exists");
}