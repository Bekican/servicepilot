using System.Net.Http.Json;

using Microsoft.Extensions.DependencyInjection;

using ServicePilot.Application.Customers;
using ServicePilot.Contracts.Authentication;
using ServicePilot.IntegrationTests.Infrastructure;

namespace ServicePilot.IntegrationTests.Customers;

[Collection(IntegrationTestCollection.Name)]
public sealed class CustomerPersistenceTests
{
    private readonly HttpClient _client;
    private readonly ServicePilotApiFactory _factory;

    public CustomerPersistenceTests(
        ServicePilotApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CustomerNumbers_ShouldBeUniqueAndSequential_UnderConcurrency()
    {
        AuthenticationTokenResponse owner =
            await RegisterOwnerAsync();

        Task<long>[] allocationTasks =
            Enumerable.Range(0, 20)
                .Select(_ =>
                    AllocateNumberAsync(
                        owner.OrganizationId))
                .ToArray();

        long[] numbers =
            await Task.WhenAll(allocationTasks);

        Assert.Equal(
            Enumerable.Range(1, 20)
                .Select(value => (long)value),
            numbers.Order());
    }

    private async Task<long> AllocateNumberAsync(
        Guid organizationId)
    {
        await using AsyncServiceScope scope =
            _factory.Services.CreateAsyncScope();
        ICustomerNumberGenerator generator =
            scope.ServiceProvider.GetRequiredService<
                ICustomerNumberGenerator>();

        return await generator.NextAsync(organizationId);
    }

    private async Task<AuthenticationTokenResponse>
        RegisterOwnerAsync()
    {
        string uniqueValue = Guid.NewGuid().ToString("N");

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest(
                    $"Customer Core {uniqueValue}",
                    $"customer-core-{uniqueValue}",
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