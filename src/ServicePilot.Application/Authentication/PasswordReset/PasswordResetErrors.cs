using ServicePilot.Application.Common;

namespace ServicePilot.Application.Authentication.PasswordReset;

public static class PasswordResetErrors
{
    public static readonly Error InvalidOrExpired = new(
        "PasswordReset.InvalidOrExpired",
        "Password reset token is invalid, expired or already used");
}