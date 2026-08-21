using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using ServicePilot.Api.Authentication;
using ServicePilot.Api.Errors;
using ServicePilot.Application.Common;
using ServicePilot.Application.Knowledge;

using ContractResponse =
    ServicePilot.Contracts.Knowledge.KnowledgeDocumentResponse;

namespace ServicePilot.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.KnowledgeUse)]
[Route("api/knowledge/documents")]
public sealed class KnowledgeDocumentsController(
    KnowledgeDocumentService service,
    ApiProblemDetailsFactory problemFactory)
    : ServicePilotControllerBase(problemFactory)
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.KnowledgeManage)]
    [EnableRateLimiting("knowledgeUpload")]
    [RequestSizeLimit(KnowledgeDocumentService.MaximumPdfBytes + 65536)]
    [ProducesResponseType(
        typeof(ContractResponse),
        StatusCodes.Status201Created)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string documentType,
        [FromForm] string? accessScope,
        CancellationToken cancellationToken)
    {
        await using Stream content = file.OpenReadStream();
        Result<KnowledgeDocumentResponse> result =
            await service.UploadAsync(
                new UploadKnowledgeDocument(
                    file.FileName,
                    file.ContentType,
                    content,
                    file.Length,
                    documentType,
                    accessScope),
                cancellationToken);

        return result.IsSuccess
            ? Created(
                $"/api/knowledge/documents/{result.Value.Id}",
                Map(result.Value))
            : ToProblem(result.Error);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ContractResponse>>> List(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<KnowledgeDocumentResponse> documents =
            await service.ListAsync(cancellationToken);
        return Ok(documents.Select(Map));
    }

    [HttpGet("{id:guid}/content")]
    public async Task<IActionResult> Open(
        Guid id,
        CancellationToken cancellationToken)
    {
        Result<KnowledgeDocumentDownload> result =
            await service.OpenAsync(id, cancellationToken);
        return result.IsSuccess
            ? File(
                result.Value.Content,
                result.Value.ContentType,
                enableRangeProcessing: true)
            : ToProblem(result.Error);
    }

    [HttpPost("{id:guid}/retry")]
    [Authorize(Policy = AuthorizationPolicies.KnowledgeManage)]
    public async Task<IActionResult> Retry(
        Guid id,
        CancellationToken cancellationToken)
    {
        Result<KnowledgeDocumentResponse> result =
            await service.RetryAsync(id, cancellationToken);
        return result.IsSuccess
            ? Ok(Map(result.Value))
            : ToProblem(result.Error);
    }

    [HttpPatch("{id:guid}/access-scope")]
    [Authorize(Policy = AuthorizationPolicies.KnowledgeManage)]
    public async Task<IActionResult> ChangeAccessScope(
        Guid id,
        ServicePilot.Contracts.Knowledge.
            ChangeKnowledgeDocumentAccessScopeRequest request,
        CancellationToken cancellationToken)
    {
        Result<KnowledgeDocumentResponse> result =
            await service.ChangeAccessScopeAsync(
                id,
                new ChangeKnowledgeDocumentAccessScope(
                    request.AccessScope),
                cancellationToken);
        return result.IsSuccess
            ? Ok(Map(result.Value))
            : ToProblem(result.Error);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.KnowledgeManage)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        Result result = await service.DeleteAsync(
            id,
            cancellationToken);
        return result.IsSuccess
            ? NoContent()
            : ToProblem(result.Error);
    }

    private static ContractResponse Map(
        KnowledgeDocumentResponse response) =>
        new(
            response.Id,
            response.OriginalFileName,
            response.SizeBytes,
            response.DocumentType,
            response.AccessScope,
            response.Status,
            response.ProcessingAttemptCount,
            response.LastErrorCode,
            response.LastErrorMessage,
            response.CreatedAtUtc,
            response.UpdatedAtUtc);
}
