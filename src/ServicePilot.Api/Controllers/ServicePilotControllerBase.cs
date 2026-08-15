using Microsoft.AspNetCore.Mvc;

using ServicePilot.Api.Errors;
using ServicePilot.Application.Common;

namespace ServicePilot.Api.Controllers;

public abstract class ServicePilotControllerBase(
    ApiProblemDetailsFactory problemFactory)
    : ControllerBase
{
    protected ObjectResult ToProblem(Error error) =>
        problemFactory.FromError(HttpContext, error);

    protected ObjectResult ToProblem(
        string code,
        int statusCode,
        string detail)
    {
        ObjectResult result = new(problemFactory.Create(
            HttpContext,
            code,
            statusCode,
            detail))
        {
            StatusCode = statusCode
        };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
