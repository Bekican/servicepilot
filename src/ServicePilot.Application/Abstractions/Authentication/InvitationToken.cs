namespace ServicePilot.Application.Abstractions.Authentication;

public sealed record InvitationToken(
    string RawValue,
    string Hash);