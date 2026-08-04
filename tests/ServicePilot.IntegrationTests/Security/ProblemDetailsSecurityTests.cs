using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using ServicePilot.Contracts.Authentication;
using ServicePilot.IntegrationTests.Infrastructure;

namespace ServicePilot.IntegrationTests.Security;

[Collection(IntegrationTestCollection.Name)]
public sealed class ProblemDetailsSecurityTests(
    ServicePilotApiFactory factory)
{
    private const string CorrelationIdHeader =
        "X-Correlation-ID";

    [Fact]
    public async Task InvalidCredentials_ShouldReturn_CorrelatedProblem()
    {
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = new(
            HttpMethod.Post,
            "/api/auth/login");
        request.Headers.Add(
            CorrelationIdHeader,
            "login-problem-1234");
        request.Content = JsonContent.Create(
            new LoginRequest(
                "missing-organization",
                "nobody@example.com",
                "wrong-password"));

        using HttpResponseMessage response =
            await client.SendAsync(request);
        using JsonDocument problem =
            await AssertProblemAsync(
                response,
                HttpStatusCode.Unauthorized,
                "Authentication.InvalidCredentials");

        Assert.Equal(
            "login-problem-1234",
            problem.RootElement
                .GetProperty("correlationId")
                .GetString());
    }

    [Fact]
    public async Task InvalidJson_ShouldNotEcho_RequestValues()
    {
        using HttpClient client = factory.CreateClient();
        const string secret =
            "password-value-must-not-be-returned";
        using StringContent content = new(
            "{\"organizationSlug\":\"tenant\","
            + "\"email\":[],"
            + $"\"password\":\"{secret}\"}}",
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response =
            await client.PostAsync(
                "/api/auth/login",
                content);
        string body = await response.Content
            .ReadAsStringAsync();
        using JsonDocument problem =
            JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "Request.ValidationFailed",
            problem.RootElement.GetProperty("code").GetString());
        Assert.True(
            problem.RootElement.TryGetProperty(
                "errors",
                out _));
        Assert.DoesNotContain(
            secret,
            body,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProtectedEndpoint_ShouldReturn_AuthenticationProblem()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response =
            await client.GetAsync("/api/customers");

        using JsonDocument problem =
            await AssertProblemAsync(
                response,
                HttpStatusCode.Unauthorized,
                "Authentication.Required");
    }

    [Fact]
    public async Task MissingEndpoint_ShouldReturn_NotFoundProblem()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response =
            await client.GetAsync("/missing/raw-value-123");

        using JsonDocument problem =
            await AssertProblemAsync(
                response,
                HttpStatusCode.NotFound,
                "Http.NotFound");
        Assert.False(
            problem.RootElement.TryGetProperty(
                "instance",
                out _));
    }

    [Fact]
    public async Task UnhandledException_ShouldReturn_SanitizedProblem()
    {
        using HttpClient client = factory.CreateClient();
        factory.LogSink.Clear();

        using HttpResponseMessage response =
            await client.GetAsync(
                "/testing/failures/unhandled");
        string body = await response.Content
            .ReadAsStringAsync();
        using JsonDocument problem =
            JsonDocument.Parse(body);

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);
        Assert.Equal(
            "System.Unexpected",
            problem.RootElement.GetProperty("code").GetString());
        Assert.DoesNotContain(
            "secret-exception-message-must-not-leak",
            body,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "InvalidOperationException",
            body,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "stack",
            body,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            factory.LogSink.Entries,
            entry => entry.Contains(
                "secret-exception-message-must-not-leak",
                StringComparison.Ordinal));
    }

    private static async Task<JsonDocument> AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        JsonDocument problem = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        JsonElement root = problem.RootElement;
        Assert.Equal(
            expectedCode,
            root.GetProperty("code").GetString());
        Assert.Equal(
            (int)expectedStatus,
            root.GetProperty("status").GetInt32());
        Assert.Matches(
            "^[a-f0-9]{32}$",
            root.GetProperty("traceId").GetString()!);

        string correlationId = root
            .GetProperty("correlationId")
            .GetString()!;
        Assert.Equal(
            correlationId,
            response.Headers
                .GetValues(CorrelationIdHeader)
                .Single());
        return problem;
    }
}
