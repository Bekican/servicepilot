namespace ServicePilot.Api.Errors;

public static class ApiProblemCodes
{
    public const string ValidationFailed =
        "Request.ValidationFailed";
    public const string AuthenticationRequired =
        "Authentication.Required";
    public const string AuthorizationForbidden =
        "Authorization.Forbidden";
    public const string HttpNotFound =
        "Http.NotFound";
    public const string RateLimitExceeded =
        "Http.RateLimitExceeded";
    public const string Unexpected =
        "System.Unexpected";
    public const string UnmappedError =
        "System.UnmappedError";
}
