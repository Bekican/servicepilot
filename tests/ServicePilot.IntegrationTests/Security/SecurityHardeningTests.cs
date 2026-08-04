using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

using ServicePilot.Contracts.Authentication;
using ServicePilot.IntegrationTests.Infrastructure;

namespace ServicePilot.IntegrationTests.Security;

[Collection(IntegrationTestCollection.Name)]
public sealed class SecurityHardeningTests(
    ServicePilotApiFactory factory)
{
    private const string CorrelationIdHeader =
        "X-Correlation-ID";

    [Fact]
    public async Task Health_ShouldBeAnonymous_AndSetSecurityHeaders()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage live =
            await client.GetAsync("/health/live");
        HttpResponseMessage ready =
            await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.Equal(
            "nosniff",
            live.Headers.GetValues(
                "X-Content-Type-Options").Single());
        Assert.Equal(
            "DENY",
            live.Headers.GetValues(
                "X-Frame-Options").Single());
        Assert.Equal(
            "no-referrer",
            live.Headers.GetValues(
                "Referrer-Policy").Single());
    }

    [Fact]
    public async Task Authentication_ShouldBeRateLimited()
    {
        await using WebApplicationFactory<Program> limitedFactory =
            new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");
                    builder.UseSetting(
                        "ConnectionStrings:Database",
                        factory.ConnectionString);
                    builder.UseSetting(
                        "Jwt:SigningKey",
                        "servicepilot-rate-limit-test-signing-key");
                    builder.UseSetting(
                        "RateLimiting:"
                        + "AuthenticationPermitLimit",
                        "2");
                });
        using HttpClient client =
            limitedFactory.CreateClient();
        LoginRequest request = new(
            "does-not-exist",
            "nobody@example.com",
            "wrong-password");

        HttpResponseMessage first =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                request);
        HttpResponseMessage second =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                request);
        HttpResponseMessage third =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            first.StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            second.StatusCode);
        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            third.StatusCode);
    }

    [Fact]
    public async Task CorrelationId_ShouldBeGenerated_WhenMissing()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response =
            await client.GetAsync("/health/live");

        string correlationId = response.Headers
            .GetValues(CorrelationIdHeader)
            .Single();
        Assert.Matches("^[a-f0-9]{32}$", correlationId);
    }

    [Fact]
    public async Task CorrelationId_ShouldPreserve_ValidWebValue()
    {
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = new(
            HttpMethod.Get,
            "/health/live");
        request.Headers.Add(
            CorrelationIdHeader,
            "web-request_1234");

        HttpResponseMessage response =
            await client.SendAsync(request);

        Assert.Equal(
            "web-request_1234",
            response.Headers
                .GetValues(CorrelationIdHeader)
                .Single());
    }

    [Fact]
    public async Task CorrelationId_ShouldReplace_UnsafeValue()
    {
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = new(
            HttpMethod.Get,
            "/health/live");
        request.Headers.TryAddWithoutValidation(
            CorrelationIdHeader,
            "unsafe value with spaces");

        HttpResponseMessage response =
            await client.SendAsync(request);

        string correlationId = response.Headers
            .GetValues(CorrelationIdHeader)
            .Single();
        Assert.NotEqual(
            "unsafe value with spaces",
            correlationId);
        Assert.Matches("^[a-f0-9]{32}$", correlationId);
    }
}
