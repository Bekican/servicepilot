using ServicePilot.Application.Common;

namespace ServicePilot.Application.Appointments;

public static class AppointmentErrors
{
    public static readonly Error NotFound = new(
        "Appointment.NotFound",
        "Appointment was not found");
    public static readonly Error InvalidData = new(
        "Appointment.InvalidData",
        "Appointment data is invalid");
    public static readonly Error CustomerUnavailable = new(
        "Appointment.CustomerUnavailable",
        "Customer is not active or was not found");
    public static readonly Error ServiceUnavailable = new(
        "Appointment.ServiceUnavailable",
        "Service is not active or was not found");
    public static readonly Error TechnicianUnavailable = new(
        "Appointment.TechnicianUnavailable",
        "Technician is not active or was not found");
    public static readonly Error TechnicianOverlap = new(
        "Appointment.TechnicianOverlap",
        "Technician already has an appointment in this time range");
    public static readonly Error InvalidTransition = new(
        "Appointment.InvalidTransition",
        "Appointment status transition is not allowed");
    public static readonly Error Forbidden = new(
        "Appointment.Forbidden",
        "User cannot perform this appointment operation");
}