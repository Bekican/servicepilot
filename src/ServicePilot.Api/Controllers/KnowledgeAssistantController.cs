using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ServicePilot.Api.Authentication;
using ServicePilot.Api.Errors;
using ServicePilot.Application.Common;
using ServicePilot.Application.Knowledge;
using ServicePilot.Contracts.Knowledge;

namespace ServicePilot.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.KnowledgeUse)]
[Route("api/knowledge/assistant")]
public sealed class KnowledgeAssistantController(
    KnowledgeAssistantService service,
    ApiProblemDetailsFactory problemFactory)
    : ServicePilotControllerBase(problemFactory)
{
    [HttpPost("ask")]
    [ProducesResponseType(
        typeof(KnowledgeAnswerResponse),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> Ask(
        AskKnowledgeRequest request,
        CancellationToken cancellationToken)
    {
        Result<KnowledgeAnswer> result = await service.AskAsync(
            request.Question,
            cancellationToken);
        return result.IsSuccess
            ? Ok(Map(result.Value))
            : ToProblem(result.Error);
    }

    private static KnowledgeAnswerResponse Map(KnowledgeAnswer answer) =>
        new(
            answer.Answer,
            answer.InsufficientEvidence,
            answer.Citations.Select(citation =>
                new KnowledgeCitationResponse(
                    citation.SourceId,
                    citation.DocumentId,
                    citation.OriginalFileName,
                    citation.PageNumber,
                    citation.ContentUrl))
                .ToArray());
}
