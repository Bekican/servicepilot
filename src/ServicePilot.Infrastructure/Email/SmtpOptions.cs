using System.Net.Mail;

using Microsoft.Extensions.Configuration;

namespace ServicePilot.Infrastructure.Email;

public sealed record SmtpOptions(
    string Host,
    int Port,
    bool EnableSsl,
    string FromAddress,
    string FromName,
    string? Username,
    string? Password)
{
    public const string SectionName = "Smtp";

    public static SmtpOptions FromConfiguration(
        IConfiguration configuration)
    {
        string host =
            configuration[$"{SectionName}:Host"]
            ?? throw new InvalidOperationException(
                "SMTP host is not configured");
        int port = configuration.GetValue<int>(
            $"{SectionName}:Port");
        bool enableSsl = configuration.GetValue<bool>(
            $"{SectionName}:EnableSsl");
        string fromAddress =
            configuration[$"{SectionName}:FromAddress"]
            ?? throw new InvalidOperationException(
                "SMTP sender address is not configured");
        string fromName =
            configuration[$"{SectionName}:FromName"]
            ?? "ServicePilot";
        string? username =
            configuration[$"{SectionName}:Username"];
        string? password =
            configuration[$"{SectionName}:Password"];

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException(
                "SMTP host cannot be empty");
        }

        if (port is <= 0 or > 65535)
        {
            throw new InvalidOperationException(
                "SMTP port is invalid");
        }

        if (!MailAddress.TryCreate(
            fromAddress,
            out _))
        {
            throw new InvalidOperationException(
                "SMTP sender address is invalid");
        }

        if (string.IsNullOrWhiteSpace(username)
            != string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "SMTP username and password must be configured together");
        }

        return new SmtpOptions(
            host,
            port,
            enableSsl,
            fromAddress,
            fromName,
            username,
            password);
    }
}