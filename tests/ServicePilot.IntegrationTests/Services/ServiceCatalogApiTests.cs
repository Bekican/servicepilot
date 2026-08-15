using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using ServicePilot.Contracts.Authentication;
using ServicePilot.Contracts.Services;
using ServicePilot.IntegrationTests.Infrastructure;

namespace ServicePilot.IntegrationTests.Services;

[Collection(IntegrationTestCollection.Name)]
public sealed class ServiceCatalogApiTests
{
    private readonly HttpClient _client;

    public ServiceCatalogApiTests(
        ServicePilotApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_ShouldEnforceNamePerTenant()
    {
        AuthenticationTokenResponse firstOwner =
            await RegisterOwnerAsync();
        AuthenticationTokenResponse secondOwner =
            await RegisterOwnerAsync();
        ServiceUpsertRequest request = new(
            "Boiler Repair",
            90);

        HttpResponseMessage first =
            await SendAsync(
                firstOwner.AccessToken,
                request);
        HttpResponseMessage duplicate =
            await SendAsync(
                firstOwner.AccessToken,
                request with
                {
                    Name = " BOILER REPAIR "
                });
        HttpResponseMessage otherTenant =
            await SendAsync(
                secondOwner.AccessToken,
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            first.StatusCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            duplicate.StatusCode);
        Assert.Equal(
            HttpStatusCode.Created,
            otherTenant.StatusCode);
    }

    [Fact]
    public async Task Status_ShouldHideInactiveServiceByDefault()
    {
        AuthenticationTokenResponse owner =
            await RegisterOwnerAsync();
        HttpResponseMessage createResponse =
            await SendAsync(
                owner.AccessToken,
                new ServiceUpsertRequest(
                    "Maintenance",
                    60));
        createResponse.EnsureSuccessStatusCode();
        ServiceResponse? service =
            await createResponse.Content.ReadFromJsonAsync<
                ServiceResponse>();
        Assert.NotNull(service);

        using HttpRequestMessage statusRequest = new(
            HttpMethod.Patch,
            $"/api/services/{service.Id}/status");
        statusRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                owner.AccessToken);
        statusRequest.Content = JsonContent.Create(
            new ServiceStatusRequest(false));

        HttpResponseMessage statusResponse =
            await _client.SendAsync(statusRequest);
        Assert.Equal(
            HttpStatusCode.OK,
            statusResponse.StatusCode);

        ServiceResponse[] active =
            await ListAsync(owner.AccessToken, false);
        ServiceResponse[] all =
            await ListAsync(owner.AccessToken, true);

        Assert.DoesNotContain(
            active,
            item => item.Id == service.Id);
        Assert.Contains(
            all,
            item =>
                item.Id == service.Id
                && !item.IsActive);
    }

    [Fact]
    public async Task DuplicateInactiveServiceName_ShouldSuggestReactivation()
    {
        AuthenticationTokenResponse owner = await RegisterOwnerAsync();
        ServiceUpsertRequest request = new("Inactive Repair", 60);
        HttpResponseMessage created = await SendAsync(owner.AccessToken, request);
        ServiceResponse service = Assert.IsType<ServiceResponse>(
            await created.Content.ReadFromJsonAsync<ServiceResponse>());
        using HttpRequestMessage deactivate = new(
            HttpMethod.Patch,
            $"/api/services/{service.Id}/status");
        deactivate.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            owner.AccessToken);
        deactivate.Content = JsonContent.Create(new ServiceStatusRequest(false));
        (await _client.SendAsync(deactivate)).EnsureSuccessStatusCode();

        HttpResponseMessage duplicate = await SendAsync(owner.AccessToken, request);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Contains(
            "Service.NameBelongsToInactiveService",
            await duplicate.Content.ReadAsStringAsync());
    }

    private async Task<HttpResponseMessage> SendAsync(
        string accessToken,
        ServiceUpsertRequest body)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Post,
            "/api/services");
        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);
        request.Content = JsonContent.Create(body);

        return await _client.SendAsync(request);
    }

    private async Task<ServiceResponse[]> ListAsync(
        string accessToken,
        bool includeInactive)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Get,
            $"/api/services?includeInactive={includeInactive}");
        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        HttpResponseMessage response =
            await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        ServiceResponse[]? services =
            await response.Content.ReadFromJsonAsync<
                ServiceResponse[]>();

        return Assert.IsType<ServiceResponse[]>(
            services);
    }

    private async Task<AuthenticationTokenResponse>
        RegisterOwnerAsync()
    {
        string uniqueValue = Guid.NewGuid().ToString("N");
        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest(
                    $"Service API {uniqueValue}",
                    $"service-api-{uniqueValue}",
                    "Owner",
                    "User",
                    $"owner-{uniqueValue}@example.com",
                    "correct-password"));
        response.EnsureSuccessStatusCode();

        AuthenticationTokenResponse? authentication =
            await response.Content.ReadFromJsonAsync<
                AuthenticationTokenResponse>();

        return Assert.IsType<
            AuthenticationTokenResponse>(
                authentication);
    }
}
