using System.Net;
using System.Net.Http.Json;

using ServicePilot.Contracts.Organizations;
using ServicePilot.Contracts.Organizations.CreateOrganization;
using ServicePilot.IntegrationTests.Infrastructure;

namespace ServicePilot.IntegrationTests.Organizations;

[Collection(IntegrationTestCollection.Name)]
public sealed class CreateOrganizationTests
{
    private readonly HttpClient _client;

    public CreateOrganizationTests(
        ServicePilotApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenRequestIsValid()
    {
        CreateOrganizationRequest request = new(
            "Acme Technical Service",
            $"acme-{Guid.NewGuid():N}");

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/organizations",
                request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        OrganizationResponse? organization =
            await response.Content.ReadFromJsonAsync<
                OrganizationResponse>();

        Assert.NotNull(organization);
        Assert.Equal(request.Name, organization.Name);
        Assert.Equal(request.Slug, organization.Slug);
        Assert.NotEqual(Guid.Empty, organization.Id);
        Assert.NotNull(response.Headers.Location);
    }


    [Fact]
    public async Task Create_ShouldReturnConflict_WhenSlugAlreadyExists()
    {
        string slug = $"duplicate-{Guid.NewGuid():N}";

        CreateOrganizationRequest firstRequest = new(
            "First Organization",
            slug);

        CreateOrganizationRequest secondRequest = new(
            "Second Organization",
            slug);

        HttpResponseMessage firstResponse =
            await _client.PostAsJsonAsync(
                "/api/organizations",
                firstRequest);

        HttpResponseMessage secondResponse =
            await _client.PostAsJsonAsync(
                "/api/organizations",
                secondRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldAllowOnlyOneRequest_WhenRequestsAreConcurrent()
    {
        string slug = $"concurrent-{Guid.NewGuid():N}";

        CreateOrganizationRequest firstRequest = new(
            "Concurrent Organization One",
            slug);

        CreateOrganizationRequest secondRequest = new(
            "Concurrent Organization Two",
            slug);

        Task<HttpResponseMessage> firstTask =
            _client.PostAsJsonAsync(
                "/api/organizations",
                firstRequest);

        Task<HttpResponseMessage> secondTask =
            _client.PostAsJsonAsync(
                "/api/organizations",
                secondRequest);

        HttpResponseMessage[] responses =
            await Task.WhenAll(firstTask, secondTask);

        Assert.Single(
            responses,
            response =>
                response.StatusCode == HttpStatusCode.Created);

        Assert.Single(
            responses,
            response =>
                response.StatusCode == HttpStatusCode.Conflict);
    }
}