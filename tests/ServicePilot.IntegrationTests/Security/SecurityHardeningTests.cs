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
}