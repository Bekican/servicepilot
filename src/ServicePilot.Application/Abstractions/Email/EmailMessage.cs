namespace ServicePilot.Application.Abstractions.Email;

public sealed record EmailMessage(
    string Recipient,
    string Subject,
    string TextBody);