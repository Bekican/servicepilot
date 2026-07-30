namespace ServicePilot.Domain.Auditing;

public static class AuditLogActions
{
    public const string OwnerRegistered =
        "User.OwnerRegistered";
    public const string InvitationCreated =
        "UserInvitation.Created";
    public const string InvitationResent =
        "UserInvitation.Resent";
    public const string InvitationAccepted =
        "UserInvitation.Accepted";
    public const string RoleChanged =
        "User.RoleChanged";
    public const string Activated =
        "User.Activated";
    public const string Deactivated =
        "User.Deactivated";
}