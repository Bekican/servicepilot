using System.Globalization;
using System.Text.RegularExpressions;

using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Abstractions.Persistence.Exceptions;
using ServicePilot.Application.Common;
using ServicePilot.Application.Customers;
using ServicePilot.Application.Services;
using ServicePilot.Application.Users;
using ServicePilot.Domain.Appointments;
using ServicePilot.Domain.Customers;
using ServicePilot.Domain.Services;
using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Appointments;

public sealed partial class AppointmentService(
    ICurrentUserContext currentUser,
    IAppointmentRepository appointmentRepository,
    ICustomerRepository customerRepository,
    IServiceCatalogRepository serviceRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<AppointmentResponse>> CreateAsync(
        CreateAppointmentData data,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseOffsetTime(
            data.StartAt,
            out DateTimeOffset startAtUtc)
            || !TryParseOffsetTime(
                data.EndAt,
                out DateTimeOffset endAtUtc)
            || endAtUtc <= startAtUtc)
        {
            return Result<AppointmentResponse>.Failure(
                AppointmentErrors.InvalidData);
        }

        Error? referenceError =
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
            timeProvider.GetUtcNow());
        appointmentRepository.Add(appointment);

        Result saveResult =
            await SaveAsync(cancellationToken);

        return saveResult.IsFailure
            ? Result<AppointmentResponse>.Failure(
                saveResult.Error)
            : Result<AppointmentResponse>.Success(
                Map(appointment));
    }

    public async Task<Result<AppointmentResponse>> GetAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        Appointment? appointment =
            await appointmentRepository.GetByIdAsync(
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

        return Result<AppointmentResponse>.Success(
            Map(appointment));
    }

    public async Task<IReadOnlyList<AppointmentResponse>>
        ListAsync(
            CancellationToken cancellationToken = default)
    {
        Guid? technicianFilter =
            currentUser.Role == UserRoles.Technician
                ? currentUser.UserId
                : null;

        IReadOnlyList<Appointment> appointments =
            await appointmentRepository.ListAsync(
                currentUser.OrganizationId,
                technicianFilter,
                cancellationToken);

        return appointments.Select(Map).ToArray();
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

        return saveResult.IsFailure
            ? Result<AppointmentResponse>.Failure(
                saveResult.Error)
            : Result<AppointmentResponse>.Success(
                Map(appointment));
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

        if (currentUser.Role == UserRoles.Technician
            && (
                appointment.TechnicianUserId
                    != currentUser.UserId
                || target is not (
                    AppointmentStatus.InProgress
                    or AppointmentStatus.Completed)
            ))
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

        return saveResult.IsFailure
            ? Result<AppointmentResponse>.Failure(
                saveResult.Error)
            : Result<AppointmentResponse>.Success(
                Map(appointment));
    }

    private async Task<Error?> ValidateReferencesAsync(
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
            return AppointmentErrors.CustomerUnavailable;
        }

        ServiceCatalogItem? service =
            await serviceRepository.GetByIdAsync(
                currentUser.OrganizationId,
                serviceId,
                cancellationToken);

        if (service is null || !service.IsActive)
        {
            return AppointmentErrors.ServiceUnavailable;
        }

        return technicianUserId is Guid technicianId
            ? await ValidateTechnicianAsync(
                technicianId,
                cancellationToken)
            : null;
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

    private static AppointmentResponse Map(
        Appointment appointment)
    {
        return new AppointmentResponse(
            appointment.Id,
            appointment.CustomerId,
            appointment.ServiceId,
            appointment.TechnicianUserId,
            appointment.StartAtUtc,
            appointment.EndAtUtc,
            appointment.Status.ToString(),
            appointment.CreatedAtUtc,
            appointment.UpdatedAtUtc);
    }

    [GeneratedRegex(
        @"(Z|[+-]\d{2}:\d{2})$",
        RegexOptions.CultureInvariant)]
    private static partial Regex OffsetSuffixRegex();
}