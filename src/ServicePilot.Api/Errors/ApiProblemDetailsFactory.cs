using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

using ServicePilot.Api.Observability;
using ServicePilot.Application.Common;

namespace ServicePilot.Api.Errors;

public sealed class ApiProblemDetailsFactory(
    ILogger<ApiProblemDetailsFactory> logger)
{
    private static readonly EventId UnmappedErrorEvent =
        new(1002, "ApiUnmappedError");

    public const string ProblemCodeItemName =
        "ServicePilot.ProblemCode";

    public ObjectResult FromError(
        HttpContext httpContext,
        Error error)
    {
        ApiProblemDescriptor descriptor;
        if (!ApiErrorCatalog.TryResolve(
                error,
                out descriptor!))
        {
            logger.LogError(
                UnmappedErrorEvent,
                "Application error code {UnmappedErrorCode} "
                + "has no public HTTP mapping",
                error.Code);
            descriptor = new ApiProblemDescriptor(
                ApiProblemCodes.UnmappedError,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.");
        }

        ProblemDetails problem = Create(
            httpContext,
            descriptor);

        ObjectResult result = new(problem)
        {
            StatusCode = descriptor.StatusCode
        };
        result.ContentTypes.Add(
            "application/problem+json");
        return result;
    }

    public ProblemDetails Create(
        HttpContext httpContext,
        string code,
        int statusCode,
        string detail) =>
        Create(
            httpContext,
            new ApiProblemDescriptor(
                code,
                statusCode,
                detail));

    public ValidationProblemDetails CreateValidation(
        HttpContext httpContext,
        IReadOnlyDictionary<string, string[]> errors)
    {
        const int statusCode =
            StatusCodes.Status400BadRequest;
        ValidationProblemDetails problem = new(
            errors.ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.Ordinal))
        {
            Type = ProblemType(
                ApiProblemCodes.ValidationFailed),
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Status = statusCode,
            Detail = "One or more request fields are invalid."
        };

        Enrich(
            httpContext,
            problem,
            ApiProblemCodes.ValidationFailed);
        return problem;
    }

    public async Task WriteAsync(
        HttpContext httpContext,
        string code,
        int statusCode,
        string detail,
        CancellationToken cancellationToken)
    {
        ProblemDetails problem = Create(
            httpContext,
            code,
            statusCode,
            detail);
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken);
    }

    private static ProblemDetails Create(
        HttpContext httpContext,
        ApiProblemDescriptor descriptor)
    {
        ProblemDetails problem = new()
        {
            Type = ProblemType(descriptor.Code),
            Title = ReasonPhrases.GetReasonPhrase(
                descriptor.StatusCode),
            Status = descriptor.StatusCode,
            Detail = descriptor.Detail
        };

        Enrich(httpContext, problem, descriptor.Code);
        return problem;
    }

    private static void Enrich(
        HttpContext httpContext,
        ProblemDetails problem,
        string code)
    {
        string correlationId =
            httpContext.Items[
                CorrelationIdMiddleware.ItemName]
                as string
            ?? httpContext.TraceIdentifier;
        Activity? activity = Activity.Current;
        string traceId = activity?
            .TraceId.ToHexString()
            ?? string.Empty;

        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = traceId;
        problem.Extensions["correlationId"] =
            correlationId;
        httpContext.Items[ProblemCodeItemName] = code;
        activity?.SetTag(
            "problem.code",
            code);

        if (problem.Status >= 500)
        {
            activity?.SetStatus(
                ActivityStatusCode.Error);
        }
    }

    private static string ProblemType(string code) =>
        $"urn:servicepilot:problem:{code}";
}
