using System.Net;
using System.Net.Mail;

using ServicePilot.Application.Abstractions.Email;

namespace ServicePilot.Infrastructure.Email;

internal sealed class SmtpEmailSender(
    SmtpOptions options)
    : IEmailSender
{
    public async Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        using MailMessage mailMessage = new()
        {
            From = new MailAddress(
                options.FromAddress,
                options.FromName),
            Subject = message.Subject,
            Body = message.TextBody,
            IsBodyHtml = false
        };

        mailMessage.To.Add(message.Recipient);

        using SmtpClient smtpClient = new(
            options.Host,
            options.Port)
        {
            EnableSsl = options.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(options.Username))
        {
            smtpClient.Credentials = new NetworkCredential(
                options.Username,
                options.Password);
        }

        try
        {
            await smtpClient.SendMailAsync(
                mailMessage,
                cancellationToken);
        }
        catch (Exception exception)
            when (exception
                is SmtpException
                or InvalidOperationException)
        {
            throw new EmailDeliveryException(
                "SMTP delivery failed",
                exception);
        }
    }
}