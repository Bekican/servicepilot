using System.Globalization;

using ServicePilot.Application.Abstractions.Email;
using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Appointments;
using ServicePilot.Application.Organizations;
using ServicePilot.Domain.Appointments;
using ServicePilot.Domain.Organizations;
using ServicePilot.Domain.Reminders;

namespace ServicePilot.Application.Reminders;

public sealed class ReminderProcessor(
    IReminderRepository reminderRepository,
    IAppointmentRepository appointmentRepository,
    IOrganizationRepository organizationRepository,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<int> ProcessDueAsync(
        int batchSize = 20,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        IReadOnlyList<Reminder> reminders =
            await reminderRepository.ClaimDueAsync(
                nowUtc,
                nowUtc - TimeSpan.FromMinutes(10),
                batchSize,
                cancellationToken);

        foreach (Reminder reminder in reminders)
        {
            Appointment? appointment =
                await appointmentRepository.GetByIdAsync(
                    reminder.OrganizationId,
                    reminder.AppointmentId,
                    cancellationToken);

            if (appointment is null
                || appointment.Status
                    == AppointmentStatus.Cancelled)
            {
                reminder.MarkSkipped(
                    timeProvider.GetUtcNow(),
                    "Appointment is unavailable or cancelled");
                await unitOfWork.SaveChangesAsync(
                    cancellationToken);
                continue;
            }

            try
            {
                Organization? organization =
                    await organizationRepository.GetByIdAsync(
                        reminder.OrganizationId,
                        cancellationToken);
                TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(
                    organization?.TimeZoneId ?? "UTC");
                DateTimeOffset localStart = TimeZoneInfo.ConvertTime(
                    appointment.StartAtUtc,
                    timeZone);

                await emailSender.SendAsync(
                    new EmailMessage(
                        reminder.RecipientEmail!,
                        "ServicePilot randevu hatırlatması",
                        "Randevunuz "
                        + localStart.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
                        + $" tarihinde başlayacaktır ({timeZone.Id})."),
                    cancellationToken);

                reminder.MarkSent(
                    timeProvider.GetUtcNow());
            }
            catch (EmailDeliveryException)
            {
                reminder.MarkDeliveryFailure(
                    timeProvider.GetUtcNow(),
                    "Email delivery failed");
            }

            await unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return reminders.Count;
    }
}