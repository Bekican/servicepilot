using ServicePilot.Application.Common;

namespace ServicePilot.Application.Users.Invitations;

public static class InvitationErrors
{
    public static readonly Error EmailIsRequired = new(
        "Invitation.EmailIsRequired",
        "Invitation email is required");

    public static readonly Error InvalidEmail = new(
        "Invitation.InvalidEmail",
        "Invitation email is invalid");

    public static readonly Error EmailTooLong = new(
        "Invitation.EmailTooLong",
        "Invitation email is too long");

    public static readonly Error InvalidRole = new(
        "Invitation.InvalidRole",
        "Invitation role is not supported");

    public static readonly Error UserAlreadyExists = new(
        "Invitation.UserAlreadyExists",
        "A user with this email already exists in the organization");

    public static readonly Error NotFound = new(
        "Invitation.NotFound",
        "Invitation was not found");

    public static readonly Error NotPending = new(
        "Invitation.NotPending",
        "Only a pending invitation can be resent");

    public static readonly Error InvalidOrExpired = new(
        "Invitation.InvalidOrExpired",
        "Invitation token is invalid, expired or already used");

    public static readonly Error TokenIsRequired = new(
        "Invitation.TokenIsRequired",
        "Invitation token is required");

    public static readonly Error ConcurrentRequest = new(
        "Invitation.ConcurrentRequest",
        "Another invitation request was completed first");

    public static readonly Error EmailDeliveryFailed = new(
        "Invitation.EmailDeliveryFailed",
        "Invitation was saved but its email could not be delivered");
}