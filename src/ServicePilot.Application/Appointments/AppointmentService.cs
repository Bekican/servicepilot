using System.Globalization;
using System.Text.RegularExpressions;

using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Abstractions.Persistence.Exceptions;
using ServicePilot.Application.Authorization;
using ServicePilot.Application.Common;
using ServicePilot.Application.Customers;
using ServicePilot.Application.Reminders;
using ServicePilot.Application.Services;
using ServicePilot.Application.Users;

using ServicePilot.Domain.Appointments;
using ServicePilot.Domain.Customers;
using ServicePilot.Domain.Reminders;
using ServicePilot.Domain.Services;
using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Appointments;

public sealed partial class AppointmentService(
    ICurrentUserContext currentUser,
    IAppointmentRepository appointmentRepository,
    ICustomerRepository customerRepository,
    IServiceCatalogRepository serviceRepository,
    IUserRepository userRepository,
    IReminderRepository reminderRepository,
    IUserAuthorizationService authorizationService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<AppointmentResponse>> CreateAsync(
        CreateAppointmentData data,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();

        if (!TryParseOffsetTime(
            data.StartAt,
            out DateTimeOffset startAtUtc)
            || !TryParseOffsetTime(
                data.EndAt,
                out DateTimeOffset endAtUtc)
            || endAtUtc <= startAtUtc
            || startAtUtc <= nowUtc)
        {
            return Result<AppointmentResponse>.Failure(
                AppointmentErrors.InvalidData);
        }

        (Customer? customer, Error? referenceError) =
            await ValidateReferencesAsync(
                data.CustomerId,
                data.ServiceId,
                data.TechnicianUserId,
                cancellationToken);

        if (referenceError is not null)
        {
            return Result<AppointmentResponse>.Failure(
                referenceError);
        }

        if (data.TechnicianUserId is Guid technicianId
            && await appointmentRepository
                .HasTechnicianOverlapAsync(
                    currentUser.OrganizationId,
                    technicianId,
                    startAtUtc,
                    endAtUtc,
                    null,
                    cancellationToken))
        {
            return Result<AppointmentResponse>.Failure(
                AppointmentErrors.TechnicianOverlap);
        }

        Appointment appointment = new(
            Guid.NewGuid(),
            currentUser.OrganizationId,
            data.CustomerId,
            data.ServiceId,
            data.TechnicianUserId,
            startAtUtc,
            endAtUtc,
            nowUtc);
        appointmentRepository.Add(appointment);

        DateTimeOffset reminderTime =
            startAtUtc - TimeSpan.FromHours(24);
        Reminder reminder = new(
            Guid.NewGuid(),
            currentUser.OrganizationId,
            appointment.Id,
            customer!.Email,
            reminderTime > nowUtc
                ? reminderTime
                : nowUtc,
            nowUtc);
        reminderRepository.Add(reminder);

        Result saveResult =
            await SaveAsync(cancellationToken);

        if (saveResult.IsFailure)
        {
            return Result<AppointmentResponse>.Failure(
                saveResult.Error);
        }

        return await GetSavedResponseAsync(
            appointment.Id,
            cancellationToken);
    }

    public async Task<Result<AppointmentResponse>> GetAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        AppointmentDetails? appointment =
            await appointmentRepository.GetDetailsAsync(
                currentUser.OrganizationId,
                appointmentId,
                cancellationToken);

        if (appointment is null
            || currentUser.Role == UserRoles.Technician
            && appointment.TechnicianUserId
                != currentUser.UserId)
        {
            return Result<AppointmentResponse>.Failure(
                AppointmentErrors.NotFound);
        }

        bool canManage =
            await CanManageAppointmentsAsync(
                cancellationToken);

        return Result<AppointmentResponse>.Success(
            Map(appointment, canManage));
    }

    public async Task<IReadOnlyList<AppointmentResponse>>
        ListAsync(
            CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<AppointmentResponse>> result =
            await ListAsync(
                new AppointmentFilterData(
                    null,
                    null,
                    null,
                    null),
                cancellationToken);

        return result.Value;
    }

    public async Task<Result<
        IReadOnlyList<AppointmentResponse>>> ListAsync(
        AppointmentFilterData filter,
        CancellationToken cancellationToken = default)
    {
        if (filter.FromUtc is DateTimeOffset fromUtc
            && filter.ToUtc is DateTimeOffset toUtc
            && toUtc <= fromUtc)
        {
            return Result<
                IReadOnlyList<AppointmentResponse>>.Failure(
                    AppointmentErrors.InvalidData);
        }

        AppointmentStatus? status = null;

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            if (!Enum.TryParse(
                filter.Status,
                true,
                out AppointmentStatus parsedStatus)
                || !Enum.IsDefined(parsedStatus))
            {
                return Result<
                    IReadOnlyList<AppointmentResponse>>
                    .Failure(
                        AppointmentErrors.InvalidData);
            }

            status = parsedStatus;
        }

        Guid? technicianFilter =
            currentUser.Role == UserRoles.Technician
                ? currentUser.UserId
                : filter.TechnicianUserId;

        IReadOnlyList<AppointmentDetails> appointments =
            await appointmentRepository.ListDetailsAsync(
                currentUser.OrganizationId,
                new AppointmentQuery(
                    filter.FromUtc?.ToUniversalTime(),
                    filter.ToUtc?.ToUniversalTime(),
                    status,
                    technicianFilter),
                cancellationToken);
        bool canManage =
            await CanManageAppointmentsAsync(
                cancellationToken);

        return Result<
            IReadOnlyList<AppointmentResponse>>.Success(
                appointments.Select(appointment =>
                    Map(appointment, canManage)).ToArray());
    }

    public async Task<Result<PageResult<AppointmentResponse>>> ListPageAsync(
        AppointmentFilterData filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (filter.FromUtc is DateTimeOffset fromUtc
            && filter.ToUtc is DateTimeOffset toUtc
            && toUtc <= fromUtc)
        {
            return Result<PageResult<AppointmentResponse>>.Failure(
                AppointmentErrors.InvalidData);
        }

        AppointmentStatus? status = null;
        if (!string.IsNullOrWhiteSpace(filter.Status)
            && (!Enum.TryParse(filter.Status, true, out AppointmentStatus parsed)
                || !Enum.IsDefined(parsed)))
        {
            return Result<PageResult<AppointmentResponse>>.Failure(
                AppointmentErrors.InvalidData);
        }
        else if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            status = Enum.Parse<AppointmentStatus>(filter.Status, true);
        }

        (page, pageSize) = PageResult<AppointmentResponse>.Normalize(page, pageSize);
        Guid? technicianFilter = currentUser.Role == UserRoles.Technician
            ? currentUser.UserId
            : filter.TechnicianUserId;
        (IReadOnlyList<AppointmentDetails> items, int totalCount) =
            await appointmentRepository.ListDetailsPageAsync(
                currentUser.OrganizationId,
                new AppointmentQuery(
                    filter.FromUtc?.ToUniversalTime(),
                    filter.ToUtc?.ToUniversalTime(),
                    status,
                    technicianFilter),
                (page - 1) * pageSize,
                pageSize,
                cancellationToken);
        bool canManage = await CanManageAppointmentsAsync(cancellationToken);
        return Result<PageResult<AppointmentResponse>>.Success(
            new PageResult<AppointmentResponse>(
                items.Select(item => Map(item, canManage)).ToArray(),
                page,
                pageSize,
                totalCount));
    }

    public async Task<Result<AppointmentResponse>>
        AssignAsync(
            Guid appointmentId,
            Guid technicianUserId,
            CancellationToken cancellationToken = default)
    {
        Appointment? appointment =
            await appointmentRepository.GetByIdAsync(
                currentUser.OrganizationId,
                appointmentId,
                cancellationToken);

        if (appointment is null)
        {
            return Result<AppointmentResponse>.Failure(
                AppointmentErrors.NotFound);
        }

        Error? technicianError =
            await ValidateTechnicianAsync(
                technicianUserId,
                cancellationToken);

        if (technicianError is not null)
        {
            return Result<AppointmentResponse>.Failure(
                technicianError);
        }

        if (await appointmentRepository
            .HasTechnicianOverlapAsync(
                currentUser.OrganizationId,
                technicianUserId,
                appointment.StartAtUtc,
                appointment.EndAtUtc,
                appointment.Id,
                cancellationToken))
        {
            return Result<AppointmentResponse>.Failure(
                AppointmentErrors.TechnicianOverlap);
        }

        try
        {
            appointment.AssignTechnician(
                technicianUserId,
                timeProvider.GetUtcNow());
        }
        catch (InvalidOperationException)
        {
            return Result<AppointmentResponse>.Failure(
                AppointmentErrors.InvalidTransition);
        }

        Result saveResult =
            await SaveAsync(cancellationToken);

        if (saveResult.IsFailure)
        {
            return Result<AppointmentResponse>.Failure(
                saveResult.Error);
        }

        return await GetSavedResponseAsync(
            appointment.Id,
            cancellationToken);
    }

    public async Task<Result<AppointmentResponse>>
        TransitionAsync(
            Guid appointmentId,
            string status,
            CancellationToken cancellationToken = default)
    {
        Appointment? appointment =
            await appointmentRepository.GetByIdAsync(
                currentUser.OrganizationId,
                appointmentId,
                cancellationToken);

        if (appointment is null)
        {
            return Result<AppointmentResponse>.Failure(
                AppointmentErrors.NotFound);
        }

        if (!Enum.TryParse(
            status,
            true,
            out AppointmentStatus target)
            || !Enum.IsDefined(target))
        {
            return Result<AppointmentResponse>.Failure(
                AppointmentErrors.InvalidTransition);
        }

        if (currentUser.Role == UserRoles.Technician)
        {
            if (appointment.TechnicianUserId
                != currentUser.UserId
                || target is not (
                    AppointmentStatus.InProgress
                    or AppointmentStatus.Completed))
            {
                return Result<AppointmentResponse>.Failure(
                    AppointmentErrors.Forbidden);
            }
        }
        else if (!await CanManageAppointmentsAsync(
            cancellationToken))
        {
            return Result<AppointmentResponse>.Failure(
                AppointmentErrors.Forbidden);
        }

        try
        {
            appointment.TransitionTo(
                target,
                timeProvider.GetUtcNow());
        }
        catch (InvalidOperationException)
        {
            return Result<AppointmentResponse>.Failure(
                AppointmentErrors.InvalidTransition);
        }

        Result saveResult =
            await SaveAsync(cancellationToken);

        if (saveResult.IsFailure)
        {
            return Result<AppointmentResponse>.Failure(
                saveResult.Error);
        }

        return await GetSavedResponseAsync(
            appointment.Id,
            cancellationToken);
    }

    private async Task<(Customer? Customer, Error? Error)>
        ValidateReferencesAsync(
        Guid customerId,
        Guid serviceId,
        Guid? technicianUserId,
        CancellationToken cancellationToken)
    {
        Customer? customer =
            await customerRepository.GetByIdAsync(
                currentUser.OrganizationId,
                customerId,
                cancellationToken);

        if (customer is null || !customer.IsActive)
        {
            return (
                null,
                AppointmentErrors.CustomerUnavailable);
        }

        ServiceCatalogItem? service =
            await serviceRepository.GetByIdAsync(
                currentUser.OrganizationId,
                serviceId,
                cancellationToken);

        if (service is null || !service.IsActive)
        {
            return (
                null,
                AppointmentErrors.ServiceUnavailable);
        }

        Error? technicianError =
            technicianUserId is Guid technicianId
                ? await ValidateTechnicianAsync(
                    technicianId,
                    cancellationToken)
                : null;

        return (customer, technicianError);
    }

    private async Task<Error?> ValidateTechnicianAsync(
        Guid technicianUserId,
        CancellationToken cancellationToken)
    {
        User? technician = await userRepository.GetByIdAsync(
            currentUser.OrganizationId,
            technicianUserId,
            cancellationToken);

        return technician is
        {
            IsActive: true,
            Role: UserRoles.Technician
        }
                ? null
                : AppointmentErrors.TechnicianUnavailable;
    }

    private async Task<Result> SaveAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(
                cancellationToken);
            return Result.Success();
        }
        catch (ConstraintViolationException exception)
            when (
                exception.ConstraintName
                == "ex_appointments_technician_overlap")
        {
            return Result.Failure(
                AppointmentErrors.TechnicianOverlap);
        }
    }

    private async Task<Result<AppointmentResponse>>
        GetSavedResponseAsync(
            Guid appointmentId,
            CancellationToken cancellationToken)
    {
        AppointmentDetails? details =
            await appointmentRepository.GetDetailsAsync(
                currentUser.OrganizationId,
                appointmentId,
                cancellationToken);

        if (details is null)
        {
            return Result<AppointmentResponse>.Failure(
                AppointmentErrors.NotFound);
        }

        bool canManage =
            await CanManageAppointmentsAsync(
                cancellationToken);

        return Result<AppointmentResponse>.Success(
            Map(details, canManage));
    }

    private Task<bool> CanManageAppointmentsAsync(
        CancellationToken cancellationToken)
    {
        return authorizationService.HasCapabilityAsync(
            currentUser.OrganizationId,
            currentUser.UserId,
            UserCapability.ManageAppointments,
            cancellationToken);
    }

    private AppointmentResponse Map(
        AppointmentDetails appointment,
        bool canManage)
    {
        IReadOnlyList<string> allowedTransitions =
            GetAllowedTransitions(
                appointment,
                canManage);
        bool canAssignTechnician =
            canManage
            && appointment.Status is not (
                AppointmentStatus.Completed
                or AppointmentStatus.Cancelled);

        return new AppointmentResponse(
            appointment.Id,
            appointment.CustomerId,
            appointment.CustomerNumber,
            appointment.CustomerDisplayName,
            appointment.CustomerHasEmail,
            appointment.ServiceId,
            appointment.ServiceName,
            appointment.TechnicianUserId,
            appointment.TechnicianDisplayName,
            appointment.StartAtUtc,
            appointment.EndAtUtc,
            appointment.Status.ToString(),
            allowedTransitions,
            canAssignTechnician,
            appointment.CreatedAtUtc,
            appointment.UpdatedAtUtc);
    }

    private IReadOnlyList<string> GetAllowedTransitions(
        AppointmentDetails appointment,
        bool canManage)
    {
        if (currentUser.Role == UserRoles.Technician)
        {
            if (appointment.TechnicianUserId
                != currentUser.UserId)
            {
                return [];
            }

            return appointment.Status switch
            {
                AppointmentStatus.Confirmed =>
                [
                    AppointmentStatus.InProgress
                        .ToString()
                ],
                AppointmentStatus.InProgress =>
                [
                    AppointmentStatus.Completed
                        .ToString()
                ],
                _ => []
            };
        }

        if (!canManage)
        {
            return [];
        }

        return appointment.Status switch
        {
            AppointmentStatus.Scheduled
                when appointment.TechnicianUserId
                    is not null =>
            [
                AppointmentStatus.Confirmed.ToString(),
                AppointmentStatus.Cancelled.ToString()
            ],
            AppointmentStatus.Scheduled =>
            [
                AppointmentStatus.Cancelled.ToString()
            ],
            AppointmentStatus.Confirmed =>
            [
                AppointmentStatus.InProgress.ToString(),
                AppointmentStatus.Cancelled.ToString()
            ],
            AppointmentStatus.InProgress =>
            [
                AppointmentStatus.Completed.ToString()
            ],
            _ => []
        };
    }

    private static bool TryParseOffsetTime(
        string value,
        out DateTimeOffset utcValue)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !OffsetSuffixRegex().IsMatch(value)
            || !DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTimeOffset parsed))
        {
            utcValue = default;
            return false;
        }

        utcValue = parsed.ToUniversalTime();
        return true;
    }

    [GeneratedRegex(
        @"(Z|[+-]\d{2}:\d{2})$",
        RegexOptions.CultureInvariant)]
    private static partial Regex OffsetSuffixRegex();
}