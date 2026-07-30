using ServicePilot.Application.Common;

namespace ServicePilot.Application.Users.Management;

public static class UserManagementErrors
{
    public static readonly Error NotFound = new(
        "User.NotFound",
        "User was not found");

    public static readonly Error InvalidRole = new(
        "User.InvalidRole",
        "User role is not supported");

    public static readonly Error SelfModificationNotAllowed = new(
        "User.SelfModificationNotAllowed",
        "An Owner cannot change their own role or status");

    public static readonly Error LastActiveOwner = new(
        "User.LastActiveOwner",
        "The last active Owner cannot be demoted or deactivated");
}