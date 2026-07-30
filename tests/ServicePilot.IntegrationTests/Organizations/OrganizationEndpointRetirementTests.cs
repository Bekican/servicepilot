using System.Net;
using System.Net.Http.Json;

using ServicePilot.Contracts.Organizations.CreateOrganization;
using ServicePilot.IntegrationTests.Infrastructure;

namespace ServicePilot.IntegrationTests.Organizations;

[Collection(IntegrationTestCollection.Name)]
public sealed class OrganizationEndpointRetirementTests
{
    private readonly HttpClient _client;

    public OrganizationEndpointRetirementTests(
        ServicePilotApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrganizationEndpoint_ShouldNotBeExposed()
    {
        CreateOrganizationRequest request = new(
            "Retired Endpoint Organization",
            $"retired-{Guid.NewGuid():N}");

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/organizations",
                request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
}