using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

using Microsoft.Extensions.DependencyInjection;

using ServicePilot.Contracts.Authentication;
using ServicePilot.Contracts.Knowledge;
using ServicePilot.Domain.Knowledge;
using ServicePilot.IntegrationTests.Infrastructure;
using IKnowledgeDocumentRepository =
    ServicePilot.Application.Knowledge.IKnowledgeDocumentRepository;

namespace ServicePilot.IntegrationTests.Knowledge;

[Collection(IntegrationTestCollection.Name)]
public sealed class KnowledgeDocumentApiTests
{
    private static readonly byte[] PdfBytes =
        Encoding.ASCII.GetBytes(
            "%PDF-1.4\n1 0 obj<</Type/Catalog>>endobj\n%%EOF");
    private readonly HttpClient _client;
    private readonly ServicePilotApiFactory _factory;

    public KnowledgeDocumentApiTests(ServicePilotApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Upload_ShouldStorePdfAndPreventTenantDuplicate()
    {
        AuthenticationTokenResponse owner = await RegisterOwnerAsync();

        HttpResponseMessage created = await UploadAsync(
            owner.AccessToken,
            "manual.pdf",
            "Manual",
            null);
        HttpResponseMessage duplicate = await UploadAsync(
            owner.AccessToken,
            "renamed.pdf",
            "Manual",
            null);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        KnowledgeDocumentResponse document =
            Assert.IsType<KnowledgeDocumentResponse>(
                await created.Content.ReadFromJsonAsync<
                    KnowledgeDocumentResponse>());
        Assert.Equal("Shared", document.AccessScope);
        Assert.Equal("Pending", document.Status);

        using HttpRequestMessage openRequest = Authorized(
            HttpMethod.Get,
            $"/api/knowledge/documents/{document.Id}/content",
            owner.AccessToken);
        HttpResponseMessage opened = await _client.SendAsync(openRequest);
        opened.EnsureSuccessStatusCode();
        Assert.Equal(
            PdfBytes,
            await opened.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Open_ShouldHideDocumentFromAnotherTenant()
    {
        AuthenticationTokenResponse firstOwner =
            await RegisterOwnerAsync();
        AuthenticationTokenResponse secondOwner =
            await RegisterOwnerAsync();
        HttpResponseMessage created = await UploadAsync(
            firstOwner.AccessToken,
            $"{Guid.NewGuid():N}.pdf",
            "TechnicalProcedure",
            null,
            Encoding.ASCII.GetBytes(
                $"%PDF-1.4\n%{Guid.NewGuid():N}\n%%EOF"));
        KnowledgeDocumentResponse document =
            Assert.IsType<KnowledgeDocumentResponse>(
                await created.Content.ReadFromJsonAsync<
                    KnowledgeDocumentResponse>());

        using HttpRequestMessage request = Authorized(
            HttpMethod.Get,
            $"/api/knowledge/documents/{document.Id}/content",
            secondOwner.AccessToken);
        HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Upload_ShouldRejectSharedCustomerReport()
    {
        AuthenticationTokenResponse owner = await RegisterOwnerAsync();

        HttpResponseMessage response = await UploadAsync(
            owner.AccessToken,
            $"{Guid.NewGuid():N}.pdf",
            "CustomerServiceReport",
            "Shared",
            Encoding.ASCII.GetBytes(
                $"%PDF-1.4\n%{Guid.NewGuid():N}\n%%EOF"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(
            "InvalidAccessScope",
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PendingDocument_ShouldBeClaimedOnceForProcessing()
    {
        AuthenticationTokenResponse owner = await RegisterOwnerAsync();
        HttpResponseMessage created = await UploadAsync(
            owner.AccessToken,
            $"{Guid.NewGuid():N}.pdf",
            "Manual",
            null,
            Encoding.ASCII.GetBytes(
                $"%PDF-1.4\n%{Guid.NewGuid():N}\n%%EOF"));
        KnowledgeDocumentResponse uploaded =
            Assert.IsType<KnowledgeDocumentResponse>(
                await created.Content.ReadFromJsonAsync<
                    KnowledgeDocumentResponse>());

        await using AsyncServiceScope scope =
            _factory.Services.CreateAsyncScope();
        IKnowledgeDocumentRepository repository =
            scope.ServiceProvider.GetRequiredService<
                IKnowledgeDocumentRepository>();
        IReadOnlyList<KnowledgeDocument> claimed =
            await repository.ClaimPendingAsync(
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddMinutes(-10),
                20);

        KnowledgeDocument document = Assert.Single(
            claimed,
            item => item.Id == uploaded.Id);
        Assert.Equal(
            KnowledgeDocumentStatus.Processing,
            document.Status);
        Assert.Equal(1, document.ProcessingAttemptCount);
    }

    private async Task<HttpResponseMessage> UploadAsync(
        string accessToken,
        string fileName,
        string documentType,
        string? accessScope,
        byte[]? content = null)
    {
        using MultipartFormDataContent form = new();
        ByteArrayContent file = new(content ?? PdfBytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(
            "application/pdf");
        form.Add(file, "file", fileName);
        form.Add(new StringContent(documentType), "documentType");
        if (accessScope is not null)
        {
            form.Add(new StringContent(accessScope), "accessScope");
        }

        using HttpRequestMessage request = Authorized(
            HttpMethod.Post,
            "/api/knowledge/documents",
            accessToken);
        request.Content = form;
        return await _client.SendAsync(request);
    }

    private static HttpRequestMessage Authorized(
        HttpMethod method,
        string path,
        string accessToken)
    {
        HttpRequestMessage request = new(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken);
        return request;
    }

    private async Task<AuthenticationTokenResponse> RegisterOwnerAsync()
    {
        string value = Guid.NewGuid().ToString("N");
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(
                $"Knowledge {value}",
                $"knowledge-{value}",
                "Owner",
                "User",
                $"owner-{value}@example.com",
                "correct-password"));
        response.EnsureSuccessStatusCode();
        return Assert.IsType<AuthenticationTokenResponse>(
            await response.Content.ReadFromJsonAsync<
                AuthenticationTokenResponse>());
    }
}
