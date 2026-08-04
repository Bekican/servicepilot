using System.Reflection;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

using ServicePilot.Api.Errors;
using ServicePilot.Application.Common;

namespace ServicePilot.ArchitectureTests;

public sealed class ApiErrorCatalogTests
{
    [Fact]
    public void EveryDeclaredApplicationError_ShouldHave_PublicMapping()
    {
        IReadOnlyList<Error> errors = typeof(Error).Assembly
            .GetTypes()
            .Where(type =>
                type.IsAbstract
                && type.IsSealed
                && type.Name.EndsWith(
                    "Errors",
                    StringComparison.Ordinal))
            .SelectMany(type => type.GetFields(
                BindingFlags.Public
                | BindingFlags.Static))
            .Where(field => field.FieldType == typeof(Error))
            .Select(field => (Error)field.GetValue(null)!)
            .ToArray();

        string[] unmappedCodes = errors
            .Where(error =>
                !ApiErrorCatalog.TryResolve(
                    error,
                    out _))
            .Select(error => error.Code)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(errors);
        Assert.Empty(unmappedCodes);
    }

    [Fact]
    public void UnknownApplicationError_ShouldFailClosed()
    {
        ApiProblemDetailsFactory factory = new(
            NullLogger<ApiProblemDetailsFactory>.Instance);
        DefaultHttpContext httpContext = new();
        Error unknown = new(
            "Unknown.Code",
            "sensitive-message-must-not-leak");

        ObjectResult result = factory.FromError(
            httpContext,
            unknown);
        ProblemDetails problem =
            Assert.IsType<ProblemDetails>(result.Value);

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            result.StatusCode);
        Assert.Equal(
            ApiProblemCodes.UnmappedError,
            problem.Extensions["code"]);
        Assert.DoesNotContain(
            "sensitive-message-must-not-leak",
            problem.Detail,
            StringComparison.Ordinal);
    }
}
