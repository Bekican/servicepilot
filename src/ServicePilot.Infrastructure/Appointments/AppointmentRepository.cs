using Microsoft.EntityFrameworkCore;

using ServicePilot.Application.Appointments;
using ServicePilot.Domain.Appointments;
using ServicePilot.Domain.Customers;
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

    public Task<AppointmentDetails?> GetDetailsAsync(
        Guid organizationId,
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        return BuildDetailsQuery(
                organizationId,
                appointmentId,
                new AppointmentQuery(
                    null,
                    null,
                    null,
                    null))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AppointmentDetails>>
        ListDetailsAsync(
        Guid organizationId,
        AppointmentQuery query,
        CancellationToken cancellationToken = default)
    {
        return await BuildDetailsQuery(
                organizationId,
                null,
                query)
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

    private IQueryable<AppointmentDetails>
        BuildDetailsQuery(
        Guid organizationId,
        Guid? appointmentId,
        AppointmentQuery query)
    {
        return
            from appointment in
                dbContext.Appointments.AsNoTracking()
            join customer in
                dbContext.Customers.AsNoTracking()
                on appointment.CustomerId equals customer.Id
            join service in
                dbContext.Services.AsNoTracking()
                on appointment.ServiceId equals service.Id
            join technician in
                dbContext.Users.AsNoTracking()
                on appointment.TechnicianUserId equals
                technician.Id into technicians
            from technician in technicians.DefaultIfEmpty()
            where appointment.OrganizationId
                == organizationId
                && customer.OrganizationId
                    == organizationId
                && service.OrganizationId
                    == organizationId
                && (
                    appointmentId == null
                    || appointment.Id
                        == appointmentId
                )
                && (
                    query.FromUtc == null
                    || appointment.StartAtUtc
                        >= query.FromUtc
                )
                && (
                    query.ToUtc == null
                    || appointment.StartAtUtc
                        < query.ToUtc
                )
                && (
                    query.Status == null
                    || appointment.Status
                        == query.Status
                )
                && (
                    query.TechnicianUserId == null
                    || appointment.TechnicianUserId
                        == query.TechnicianUserId
                )
            orderby appointment.StartAtUtc
            select new AppointmentDetails(
                appointment.Id,
                customer.Id,
                customer.CustomerNumber,
                customer.Type == CustomerType.Company
                    ? customer.CompanyName!
                    : customer.FirstName
                        + " "
                        + customer.LastName,
                service.Id,
                service.Name,
                appointment.TechnicianUserId,
                technician == null
                    ? null
                    : technician.FirstName
                        + " "
                        + technician.LastName,
                appointment.StartAtUtc,
                appointment.EndAtUtc,
                appointment.Status,
                appointment.CreatedAtUtc,
                appointment.UpdatedAtUtc);
    }
}