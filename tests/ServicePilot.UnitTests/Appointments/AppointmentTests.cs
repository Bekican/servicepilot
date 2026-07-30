using ServicePilot.Domain.Appointments;

namespace ServicePilot.UnitTests.Appointments;

public sealed class AppointmentTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_ShouldStoreUtcHalfOpenRange()
    {
        Appointment appointment = Create(
            technicianUserId: null);

        Assert.Equal(
            TimeSpan.Zero,
            appointment.StartAtUtc.Offset);
        Assert.Equal(
            AppointmentStatus.Scheduled,
            appointment.Status);
    }

    [Fact]
    public void Confirm_ShouldRequireTechnician()
    {
        Appointment appointment = Create(
            technicianUserId: null);

        Assert.Throws<InvalidOperationException>(() =>
            appointment.TransitionTo(
                AppointmentStatus.Confirmed,
                UtcNow));
    }

    [Fact]
    public void StateMachine_ShouldAllowHappyPath()
    {
        Appointment appointment = Create(Guid.NewGuid());

        appointment.TransitionTo(
            AppointmentStatus.Confirmed,
            UtcNow);
        appointment.TransitionTo(
            AppointmentStatus.InProgress,
            UtcNow);
        appointment.TransitionTo(
            AppointmentStatus.Completed,
            UtcNow);

        Assert.Equal(
            AppointmentStatus.Completed,
            appointment.Status);
        Assert.False(appointment.BlocksTechnicianSlot);
    }

    [Fact]
    public void Cancelled_ShouldBeTerminal()
    {
        Appointment appointment = Create(Guid.NewGuid());
        appointment.TransitionTo(
            AppointmentStatus.Cancelled,
            UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            appointment.TransitionTo(
                AppointmentStatus.Confirmed,
                UtcNow));
    }

    private static Appointment Create(
        Guid? technicianUserId)
    {
        return new Appointment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            technicianUserId,
            new DateTimeOffset(
                2026,
                7,
                30,
                15,
                0,
                0,
                TimeSpan.FromHours(3)),
            new DateTimeOffset(
                2026,
                7,
                30,
                16,
                0,
                0,
                TimeSpan.FromHours(3)),
            UtcNow);
    }
}