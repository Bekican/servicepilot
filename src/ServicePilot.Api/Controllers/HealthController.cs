using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("health")]
public sealed class HealthController(
    ServicePilotDbContext dbContext)
    : ControllerBase
{
    [HttpGet("live")]
    public IActionResult Live()
    {
        return Ok(new
        {
            status = "Healthy"
        });
    }

    [HttpGet("ready")]
    public async Task<IActionResult> Ready(
        CancellationToken cancellationToken)
    {
        bool canConnect =
            await dbContext.Database.CanConnectAsync(
                cancellationToken);

        return canConnect
            ? Ok(new
            {
                status = "Healthy"
            })
            : StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    status = "Unhealthy"
                });
    }
}