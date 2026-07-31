using ServicePilot.Domain.Appointments;

namespace ServicePilot.Application.Appointments;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(
        Guid organizationId,
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task<AppointmentDetails?> GetDetailsAsync(
        Guid organizationId,
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppointmentDetails>>
        ListDetailsAsync(
        Guid organizationId,
        AppointmentQuery query,
        CancellationToken cancellationToken = default);

    Task<bool> HasTechnicianOverlapAsync(
        Guid organizationId,
        Guid technicianUserId,
        DateTimeOffset startAtUtc,
        DateTimeOffset endAtUtc,
        Guid? excludedAppointmentId,
        CancellationToken cancellationToken = default);

    void Add(Appointment appointment);
}