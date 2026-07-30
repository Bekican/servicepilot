using System.Text.Json;

using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Abstractions.Persistence.Exceptions;
using ServicePilot.Application.Auditing;
using ServicePilot.Application.Common;
using ServicePilot.Domain.Auditing;
using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Users.Management.ChangeUserRole;

public sealed class ChangeUserRoleHandler(
    ICurrentUserContext currentUser,
    IUserRepository userRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<UserResponse>> HandleAsync(
        ChangeUserRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        string? normalizedRole =
            UserRoles.Normalize(command.Role ?? string.Empty);

        if (normalizedRole is null)
        {
            return Result<UserResponse>.Failure(
                UserManagementErrors.InvalidRole);
        }

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

        if (target.Role == normalizedRole)
        {
            return Result<UserResponse>.Success(
                UserResponse.FromUser(target));
        }

        if (target.IsActive
            && target.Role == UserRoles.Owner
            && normalizedRole != UserRoles.Owner
            && await IsLastActiveOwnerAsync(
                cancellationToken))
        {
            return Result<UserResponse>.Failure(
                UserManagementErrors.LastActiveOwner);
        }

        string previousRole = target.Role;
        DateTimeOffset occurredAtUtc =
            timeProvider.GetUtcNow();

        target.ChangeRole(normalizedRole);

        auditLogRepository.Add(new AuditLog(
            Guid.NewGuid(),
            currentUser.OrganizationId,
            currentUser.UserId,
            AuditLogActions.RoleChanged,
            nameof(User),
            target.Id,
            occurredAtUtc,
            JsonSerializer.Serialize(new
            {
                PreviousRole = previousRole,
                NewRole = normalizedRole
            })));

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