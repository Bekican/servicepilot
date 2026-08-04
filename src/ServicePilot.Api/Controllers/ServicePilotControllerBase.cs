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
}
