using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Abstractions.Email;
using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Abstractions.Persistence.Exceptions;
using ServicePilot.Application.Abstractions.Tenancy;
using ServicePilot.Application.Auditing;
using ServicePilot.Application.Authentication;
using ServicePilot.Application.Common;
using ServicePilot.Domain.Auditing;
using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Users.Invitations.CreateInvitation;

public sealed class CreateInvitationHandler(
    ITenantContext tenantContext,
    ICurrentUserContext currentUser,
    IUserRepository userRepository,
    IUserInvitationRepository invitationRepository,
    IInvitationTokenService tokenService,
    IInvitationLinkBuilder linkBuilder,
    IEmailSender emailSender,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<InvitationResponse>> HandleAsync(
        CreateInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        string email =
            AuthenticationRules.NormalizeEmail(
                command.Email ?? string.Empty);
        string role = command.Role?.Trim() ?? string.Empty;

        Error? validationError =
            InvitationRules.Validate(email, role);

        if (validationError is not null)
        {
            return Result<InvitationResponse>.Failure(
                validationError);
        }

        Guid organizationId = tenantContext.OrganizationId;

        User? existingUser =
            await userRepository.GetByOrganizationAndEmailAsync(
                organizationId,
                email,
                cancellationToken);

        if (existingUser is not null)
        {
            return Result<InvitationResponse>.Failure(
                InvitationErrors.UserAlreadyExists);
        }

        DateTimeOffset issuedAtUtc = timeProvider.GetUtcNow();
        DateTimeOffset expiresAtUtc =
            issuedAtUtc.AddHours(24);
        InvitationToken token = tokenService.Create();

        UserInvitation? invitation =
            await invitationRepository.GetPendingByEmailAsync(
                organizationId,
                email,
                cancellationToken);

        string auditAction;

        if (invitation is null)
        {
            invitation = new UserInvitation(
                Guid.NewGuid(),
                organizationId,
                email,
                role,
                token.Hash,
                issuedAtUtc,
                expiresAtUtc);

            invitationRepository.Add(invitation);
            auditAction =
                AuditLogActions.InvitationCreated;
        }
        else
        {
            invitation.Renew(
                role,
                token.Hash,
                issuedAtUtc,
                expiresAtUtc);
            auditAction =
                AuditLogActions.InvitationResent;
        }

        auditLogRepository.Add(new AuditLog(
            Guid.NewGuid(),
            organizationId,
            currentUser.UserId,
            auditAction,
            nameof(UserInvitation),
            invitation.Id,
            issuedAtUtc));

        try
        {
            await unitOfWork.SaveChangesAsync(
                cancellationToken);
        }
        catch (UniqueConstraintViolationException exception)
            when (
                exception.ConstraintName
                is "ux_user_invitations_pending_email"
                or "ux_user_invitations_token_hash")
        {
            return Result<InvitationResponse>.Failure(
                InvitationErrors.ConcurrentRequest);
        }

        string invitationLink =
            linkBuilder.Build(token.RawValue);

        try
        {
            await emailSender.SendAsync(
                InvitationRules.CreateEmail(
                    invitation.Email,
                    invitationLink,
                    invitation.ExpiresAtUtc),
                cancellationToken);
        }
        catch (EmailDeliveryException)
        {
            return Result<InvitationResponse>.Failure(
                InvitationErrors.EmailDeliveryFailed);
        }

        return Result<InvitationResponse>.Success(
            MapResponse(invitation));
    }

    private static InvitationResponse MapResponse(
        UserInvitation invitation)
    {
        return new InvitationResponse(
            invitation.Id,
            invitation.Email,
            invitation.Role,
            invitation.ExpiresAtUtc);
    }
}