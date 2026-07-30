using ServicePilot.Domain.Appointments;

namespace ServicePilot.Application.Appointments;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(
        Guid organizationId,
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> ListAsync(
        Guid organizationId,
        Guid? technicianUserId,
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