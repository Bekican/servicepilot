namespace ServicePilot.Domain.Appointments;

public sealed class Appointment
{
    private Appointment()
    {
    }

    public Appointment(
        Guid id,
        Guid organizationId,
        Guid customerId,
        Guid serviceId,
        Guid? technicianUserId,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        DateTimeOffset createdAtUtc)
    {
        ValidateIdentifier(id, nameof(id));
        ValidateIdentifier(
            organizationId,
            nameof(organizationId));
        ValidateIdentifier(
            customerId,
            nameof(customerId));
        ValidateIdentifier(
            serviceId,
            nameof(serviceId));

        if (technicianUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Technician identifier cannot be empty",
                nameof(technicianUserId));
        }

        DateTimeOffset startAtUtc =
            startAt.ToUniversalTime();
        DateTimeOffset endAtUtc =
            endAt.ToUniversalTime();

        if (endAtUtc <= startAtUtc)
        {
            throw new ArgumentException(
                "Appointment end must be after start",
                nameof(endAt));
        }

        Id = id;
        OrganizationId = organizationId;
        CustomerId = customerId;
        ServiceId = serviceId;
        TechnicianUserId = technicianUserId;
        StartAtUtc = startAtUtc;
        EndAtUtc = endAtUtc;
        Status = AppointmentStatus.Scheduled;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid ServiceId { get; private set; }
    public Guid? TechnicianUserId { get; private set; }
    public DateTimeOffset StartAtUtc { get; private set; }
    public DateTimeOffset EndAtUtc { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public bool BlocksTechnicianSlot =>
        Status is AppointmentStatus.Scheduled
            or AppointmentStatus.Confirmed
            or AppointmentStatus.InProgress;

    public void AssignTechnician(
        Guid technicianUserId,
        DateTimeOffset updatedAtUtc)
    {
        ValidateIdentifier(
            technicianUserId,
            nameof(technicianUserId));

        if (Status is AppointmentStatus.Completed
            or AppointmentStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Terminal appointment cannot be assigned");
        }

        TechnicianUserId = technicianUserId;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void TransitionTo(
        AppointmentStatus targetStatus,
        DateTimeOffset updatedAtUtc)
    {
        bool isAllowed = (Status, targetStatus) switch
        {
            (
                AppointmentStatus.Scheduled,
                AppointmentStatus.Confirmed
            ) => true,
            (
                AppointmentStatus.Scheduled,
                AppointmentStatus.Cancelled
            ) => true,
            (
                AppointmentStatus.Confirmed,
                AppointmentStatus.InProgress
            ) => true,
            (
                AppointmentStatus.Confirmed,
                AppointmentStatus.Cancelled
            ) => true,
            (
                AppointmentStatus.InProgress,
                AppointmentStatus.Completed
            ) => true,
            _ => false
        };

        if (!isAllowed)
        {
            throw new InvalidOperationException(
                "Appointment status transition is not allowed");
        }

        if (targetStatus == AppointmentStatus.Confirmed
            && TechnicianUserId is null)
        {
            throw new InvalidOperationException(
                "Confirmed appointment requires a technician");
        }

        Status = targetStatus;
        UpdatedAtUtc = updatedAtUtc;
    }

    private static void ValidateIdentifier(
        Guid identifier,
        string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException(
                "Identifier cannot be empty",
                parameterName);
        }
    }
}