namespace ServicePilot.Api.Errors;

public sealed record ApiProblemDescriptor(
    string Code,
    int StatusCode,
    string Detail);
