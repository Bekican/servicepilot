using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Abstractions.Email;
using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Abstractions.Persistence.Exceptions;
using ServicePilot.Application.Abstractions.Tenancy;
using ServicePilot.Application.Common;
using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Users.Invitations.ResendInvitation;

public sealed class ResendInvitationHandler(
    ITenantContext tenantContext,
    IUserRepository userRepository,
    IUserInvitationRepository invitationRepository,
    IInvitationTokenService tokenService,
    IInvitationLinkBuilder linkBuilder,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<InvitationResponse>> HandleAsync(
        ResendInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        UserInvitation? invitation =
            await invitationRepository.GetByIdAsync(
                tenantContext.OrganizationId,
                command.InvitationId,
                cancellationToken);

        if (invitation is null)
        {
            return Result<InvitationResponse>.Failure(
                InvitationErrors.NotFound);
        }

        if (invitation.Status
            != UserInvitationStatus.Pending)
        {
            return Result<InvitationResponse>.Failure(
                InvitationErrors.NotPending);
        }

        User? existingUser =
            await userRepository.GetByOrganizationAndEmailAsync(
                tenantContext.OrganizationId,
                invitation.Email,
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

        invitation.Renew(
            invitation.Role,
            token.Hash,
            issuedAtUtc,
            expiresAtUtc);

        try
        {
            await unitOfWork.SaveChangesAsync(
                cancellationToken);
        }
        catch (UniqueConstraintViolationException exception)
            when (
                exception.ConstraintName
                == "ux_user_invitations_token_hash")
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
            new InvitationResponse(
                invitation.Id,
                invitation.Email,
                invitation.Role,
                invitation.ExpiresAtUtc));
    }
}