using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Abstractions.Persistence.Exceptions;
using ServicePilot.Application.Auditing;
using ServicePilot.Application.Common;
using ServicePilot.Domain.Auditing;
using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Users.Management.ChangeUserStatus;

public sealed class ChangeUserStatusHandler(
    ICurrentUserContext currentUser,
    IUserRepository userRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<UserResponse>> HandleAsync(
        ChangeUserStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        User? target = await userRepository.GetByIdAsync(
            currentUser.OrganizationId,
            command.UserId,
            cancellationToken);

        if (target is null)
        {
            return Result<UserResponse>.Failure(
                UserManagementErrors.NotFound);
        }

        if (target.Id == currentUser.UserId)
        {
            return Result<UserResponse>.Failure(
                UserManagementErrors.SelfModificationNotAllowed);
        }

        if (target.IsActive == command.IsActive)
        {
            return Result<UserResponse>.Success(
                UserResponse.FromUser(target));
        }

        if (!command.IsActive
            && target.Role == UserRoles.Owner
            && await IsLastActiveOwnerAsync(
                cancellationToken))
        {
            return Result<UserResponse>.Failure(
                UserManagementErrors.LastActiveOwner);
        }

        DateTimeOffset occurredAtUtc =
            timeProvider.GetUtcNow();

        if (command.IsActive)
        {
            target.Activate();
        }
        else
        {
            target.Deactivate();
        }

        auditLogRepository.Add(new AuditLog(
            Guid.NewGuid(),
            currentUser.OrganizationId,
            currentUser.UserId,
            command.IsActive
                ? AuditLogActions.Activated
                : AuditLogActions.Deactivated,
            nameof(User),
            target.Id,
            occurredAtUtc));

        try
        {
            await unitOfWork.SaveChangesAsync(
                cancellationToken);
        }
        catch (ConstraintViolationException exception)
            when (
                exception.ConstraintName
                == "ck_users_last_active_owner")
        {
            return Result<UserResponse>.Failure(
                UserManagementErrors.LastActiveOwner);
        }

        return Result<UserResponse>.Success(
            UserResponse.FromUser(target));
    }

    private async Task<bool> IsLastActiveOwnerAsync(
        CancellationToken cancellationToken)
    {
        int activeOwnerCount =
            await userRepository.CountActiveOwnersAsync(
                currentUser.OrganizationId,
                cancellationToken);

        return activeOwnerCount <= 1;
    }
}