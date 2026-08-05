using System.Diagnostics;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

using ServicePilot.Api.Errors;

namespace ServicePilot.ArchitectureTests;

public sealed class ProblemTraceContractTests
{
    [Theory]
    [InlineData(
        StatusCodes.Status400BadRequest,
        ActivityStatusCode.Unset)]
    [InlineData(
        StatusCodes.Status500InternalServerError,
        ActivityStatusCode.Error)]
    public void Create_ShouldAddSafeCode_AndSetExpectedSpanStatus(
        int statusCode,
        ActivityStatusCode expectedStatus)
    {
        const string problemCode =
            "System.SafeTestCode";
        ApiProblemDetailsFactory factory = new(
            NullLogger<ApiProblemDetailsFactory>.Instance);
        DefaultHttpContext context = new();
        using Activity activity =
            new Activity("test.request").Start();

        factory.Create(
            context,
            problemCode,
            statusCode,
            "A safe detail.");

        Assert.Equal(
            problemCode,
            activity.GetTagItem("problem.code"));
        Assert.Equal(expectedStatus, activity.Status);
        Assert.Null(activity.StatusDescription);
    }
}
