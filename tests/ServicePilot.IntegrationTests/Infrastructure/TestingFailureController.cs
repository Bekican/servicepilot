using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServicePilot.IntegrationTests.Infrastructure;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[AllowAnonymous]
[Route("testing/failures")]
public sealed class TestingFailureController : ControllerBase
{
    [HttpGet("unhandled")]
    public IActionResult ThrowUnhandled()
    {
        throw new InvalidOperationException(
            "secret-exception-message-must-not-leak");
    }
}
