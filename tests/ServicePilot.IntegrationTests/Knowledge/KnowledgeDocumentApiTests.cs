using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using ServicePilot.Contracts.Authentication;
using ServicePilot.Contracts.Knowledge;
using ServicePilot.Domain.Knowledge;
using ServicePilot.IntegrationTests.Infrastructure;

using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

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

    [Fact]
    public async Task Processor_ShouldExtractChunkEmbedAndPublishAtomically()
    {
        AuthenticationTokenResponse owner = await RegisterOwnerAsync();
        HttpResponseMessage created = await UploadAsync(
            owner.AccessToken,
            $"{Guid.NewGuid():N}.pdf",
            "TechnicalProcedure",
            null,
            CreateTextPdf());
        KnowledgeDocumentResponse uploaded =
            Assert.IsType<KnowledgeDocumentResponse>(
                await created.Content.ReadFromJsonAsync<
                    KnowledgeDocumentResponse>());

        await using (AsyncServiceScope scope =
            _factory.Services.CreateAsyncScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<
                ServicePilot.Application.Knowledge.
                    KnowledgeDocumentIngestionProcessor>();
            await processor.ProcessPendingAsync(batchSize: 20);
        }

        (int Count, int Page, int Dimensions, string Model) indexed =
            await _factory.ExecuteDbContextAsync(async dbContext =>
            {
                await dbContext.Database.OpenConnectionAsync();
                NpgsqlConnection connection = (NpgsqlConnection)
                    dbContext.Database.GetDbConnection();
                await using NpgsqlCommand command = new(
                    """
                    SELECT count(*)::int,
                           min(page_number)::int,
                           min(vector_dims(embedding))::int,
                           min(embedding_model)
                    FROM knowledge_document_chunks
                    WHERE document_id = $1
                    """,
                    connection);
                command.Parameters.AddWithValue(uploaded.Id);
                await using NpgsqlDataReader reader =
                    await command.ExecuteReaderAsync();
                Assert.True(await reader.ReadAsync());
                return (
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetString(3));
            });

        Assert.True(indexed.Count > 0);
        Assert.Equal(1, indexed.Page);
        Assert.Equal(1024, indexed.Dimensions);
        Assert.Equal("fake-embedding-model", indexed.Model);

        KnowledgeDocumentStatus status =
            await _factory.ExecuteDbContextAsync(async dbContext =>
                await dbContext.KnowledgeDocuments
                    .Where(document => document.Id == uploaded.Id)
                    .Select(document => document.Status)
                    .SingleAsync());
        Assert.Equal(KnowledgeDocumentStatus.Ready, status);
    }

    [Fact]
    public async Task Ask_ShouldRetrieveOwnTenantAndReturnSourceLink()
    {
        AuthenticationTokenResponse firstOwner =
            await RegisterOwnerAsync();
        AuthenticationTokenResponse secondOwner =
            await RegisterOwnerAsync();
        HttpResponseMessage created = await UploadAsync(
            firstOwner.AccessToken,
            $"{Guid.NewGuid():N}.pdf",
            "TechnicalProcedure",
            "Shared",
            CreateTextPdf());
        KnowledgeDocumentResponse uploaded =
            Assert.IsType<KnowledgeDocumentResponse>(
                await created.Content.ReadFromJsonAsync<
                    KnowledgeDocumentResponse>());

        await using (AsyncServiceScope scope =
            _factory.Services.CreateAsyncScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<
                ServicePilot.Application.Knowledge.
                    KnowledgeDocumentIngestionProcessor>();
            await processor.ProcessPendingAsync(batchSize: 20);
        }

        using HttpRequestMessage ownRequest = Authorized(
            HttpMethod.Post,
            "/api/knowledge/assistant/ask",
            firstOwner.AccessToken);
        ownRequest.Content = JsonContent.Create(
            new AskKnowledgeRequest("Bakım nasıl yapılır?"));
        HttpResponseMessage ownResponse =
            await _client.SendAsync(ownRequest);
        ownResponse.EnsureSuccessStatusCode();
        KnowledgeAnswerResponse ownAnswer =
            Assert.IsType<KnowledgeAnswerResponse>(
                await ownResponse.Content.ReadFromJsonAsync<
                    KnowledgeAnswerResponse>());

        KnowledgeCitationResponse citation =
            Assert.Single(ownAnswer.Citations);
        Assert.Equal(uploaded.Id, citation.DocumentId);
        Assert.Equal(1, citation.PageNumber);
        Assert.Equal(
            $"/api/knowledge/documents/{uploaded.Id}/content#page=1",
            citation.ContentUrl);

        using HttpRequestMessage otherTenantRequest = Authorized(
            HttpMethod.Post,
            "/api/knowledge/assistant/ask",
            secondOwner.AccessToken);
        otherTenantRequest.Content = JsonContent.Create(
            new AskKnowledgeRequest("Bakım nasıl yapılır?"));
        HttpResponseMessage otherTenantResponse =
            await _client.SendAsync(otherTenantRequest);
        otherTenantResponse.EnsureSuccessStatusCode();
        KnowledgeAnswerResponse otherTenantAnswer =
            Assert.IsType<KnowledgeAnswerResponse>(
                await otherTenantResponse.Content.ReadFromJsonAsync<
                    KnowledgeAnswerResponse>());

        Assert.True(otherTenantAnswer.InsufficientEvidence);
        Assert.Empty(otherTenantAnswer.Citations);
    }

    [Fact]
    public async Task Retrieval_ShouldEnforceAccessScopeInsideSqlQuery()
    {
        AuthenticationTokenResponse owner = await RegisterOwnerAsync();
        HttpResponseMessage created = await UploadAsync(
            owner.AccessToken,
            $"{Guid.NewGuid():N}.pdf",
            "Warranty",
            "Operations",
            CreateTextPdf());
        KnowledgeDocumentResponse uploaded =
            Assert.IsType<KnowledgeDocumentResponse>(
                await created.Content.ReadFromJsonAsync<
                    KnowledgeDocumentResponse>());

        await using AsyncServiceScope scope =
            _factory.Services.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<
            ServicePilot.Application.Knowledge.
                KnowledgeDocumentIngestionProcessor>();
        await processor.ProcessPendingAsync(batchSize: 20);

        ServicePilot.Infrastructure.Persistence.ServicePilotDbContext
            dbContext = scope.ServiceProvider.GetRequiredService<
                ServicePilot.Infrastructure.Persistence.
                    ServicePilotDbContext>();
        Guid organizationId = await dbContext.KnowledgeDocuments
            .Where(document => document.Id == uploaded.Id)
            .Select(document => document.OrganizationId)
            .SingleAsync();
        var repository = scope.ServiceProvider.GetRequiredService<
            ServicePilot.Application.Knowledge.
                IKnowledgeRetrievalRepository>();
        float[] queryEmbedding = new float[1024];
        queryEmbedding[0] = 1;

        var sharedOnly = await repository.SearchAsync(
            organizationId,
            [KnowledgeDocumentAccessScope.Shared],
            queryEmbedding,
            "fake-embedding-model",
            1024,
            12);
        var operations = await repository.SearchAsync(
            organizationId,
            [KnowledgeDocumentAccessScope.Operations],
            queryEmbedding,
            "fake-embedding-model",
            1024,
            12);

        Assert.Empty(sharedOnly);
        Assert.NotEmpty(operations);
        Assert.All(
            operations,
            chunk => Assert.Equal(uploaded.Id, chunk.DocumentId));
    }

    [Fact]
    public async Task Processor_ShouldExposeTextlessPdfFailure()
    {
        AuthenticationTokenResponse owner = await RegisterOwnerAsync();
        HttpResponseMessage created = await UploadAsync(
            owner.AccessToken,
            $"{Guid.NewGuid():N}.pdf",
            "Manual",
            null,
            CreateBlankPdf());
        KnowledgeDocumentResponse uploaded =
            Assert.IsType<KnowledgeDocumentResponse>(
                await created.Content.ReadFromJsonAsync<
                    KnowledgeDocumentResponse>());

        await using (AsyncServiceScope scope =
            _factory.Services.CreateAsyncScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<
                ServicePilot.Application.Knowledge.
                    KnowledgeDocumentIngestionProcessor>();
            await processor.ProcessPendingAsync(batchSize: 20);
        }

        (KnowledgeDocumentStatus Status, string? ErrorCode) result =
            await _factory.ExecuteDbContextAsync(async dbContext =>
                await dbContext.KnowledgeDocuments
                    .Where(document => document.Id == uploaded.Id)
                    .Select(document => new ValueTuple<
                        KnowledgeDocumentStatus,
                        string?>(
                            document.Status,
                            document.LastErrorCode))
                    .SingleAsync());

        Assert.Equal(KnowledgeDocumentStatus.Failed, result.Status);
        Assert.Equal(
            "KnowledgeDocument.TextlessPdf",
            result.ErrorCode);
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

    private static byte[] CreateTextPdf()
    {
        PdfDocumentBuilder builder = new();
        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        var font = builder.AddStandard14Font(
            Standard14Font.Helvetica);
        page.AddText(
            "Kombi filtresi normal kullanimda alti ayda bir kontrol edilmelidir.",
            12,
            new PdfPoint(50, 700),
            font);
        return builder.Build();
    }

    private static byte[] CreateBlankPdf()
    {
        PdfDocumentBuilder builder = new();
        builder.AddPage(PageSize.A4);
        return builder.Build();
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
