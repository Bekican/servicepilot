namespace ServicePilot.Domain.Users;

public static class UserRoles
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Dispatcher = "Dispatcher";
    public const string Technician = "Technician";

    public static bool IsSupported(string role)
    {
        return role is Owner or Admin or Dispatcher or Technician;
    }
}