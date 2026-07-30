using Microsoft.EntityFrameworkCore;

using ServicePilot.Application.Appointments;
using ServicePilot.Domain.Appointments;
using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.Infrastructure.Appointments;

internal sealed class AppointmentRepository(
    ServicePilotDbContext dbContext)
    : IAppointmentRepository
{
    public Task<Appointment?> GetByIdAsync(
        Guid organizationId,
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Appointments.SingleOrDefaultAsync(
            appointment =>
                appointment.OrganizationId == organizationId
                && appointment.Id == appointmentId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> ListAsync(
        Guid organizationId,
        Guid? technicianUserId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .AsNoTracking()
            .Where(appointment =>
                appointment.OrganizationId == organizationId
                && (
                    technicianUserId == null
                    || appointment.TechnicianUserId
                        == technicianUserId
                ))
            .OrderBy(appointment =>
                appointment.StartAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> HasTechnicianOverlapAsync(
        Guid organizationId,
        Guid technicianUserId,
        DateTimeOffset startAtUtc,
        DateTimeOffset endAtUtc,
        Guid? excludedAppointmentId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Appointments.AnyAsync(
            appointment =>
                appointment.OrganizationId == organizationId
                && appointment.TechnicianUserId
                    == technicianUserId
                && appointment.Status
                    != AppointmentStatus.Completed
                && appointment.Status
                    != AppointmentStatus.Cancelled
                && appointment.StartAtUtc < endAtUtc
                && startAtUtc < appointment.EndAtUtc
                && (
                    excludedAppointmentId == null
                    || appointment.Id
                        != excludedAppointmentId
                ),
            cancellationToken);
    }

    public void Add(Appointment appointment)
    {
        dbContext.Appointments.Add(appointment);
    }
}