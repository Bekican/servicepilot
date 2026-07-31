namespace ServicePilot.Api.Authentication;

internal static class AuthorizationPolicies
{
    public const string ActiveUser = "ActiveUser";
    public const string ManageUsers = "ManageUsers";
    public const string CustomerWrite = "CustomerWrite";
    public const string ServiceWrite = "ServiceWrite";
    public const string AppointmentManage =
        "AppointmentManage";
    public const string DashboardView =
        "DashboardView";
    public const string ReminderRetry =
        "ReminderRetry";
}