using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;

using ServicePilot.Contracts.Authentication;
using ServicePilot.Contracts.Common;
using ServicePilot.Contracts.Customers;
using ServicePilot.IntegrationTests.Infrastructure;

namespace ServicePilot.IntegrationTests.Customers;

[Collection(IntegrationTestCollection.Name)]
public sealed class CustomerApiTests
{
    private readonly HttpClient _client;
    private readonly ServicePilotApiFactory _factory;

    public CustomerApiTests(
        ServicePilotApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateListAndDeactivate_ShouldPreserveCustomer()
    {
        AuthenticationTokenResponse owner =
            await RegisterOwnerAsync();
        CustomerUpsertRequest createRequest =
            IndividualRequest(
                $"customer-{Guid.NewGuid():N}@example.com",
                "+905551112233");

        HttpResponseMessage createResponse =
            await SendAsync(
                HttpMethod.Post,
                "/api/customers",
                owner.AccessToken,
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        CustomerResponse? customer =
            await createResponse.Content.ReadFromJsonAsync<
                CustomerResponse>();
        Assert.NotNull(customer);
        Assert.Equal("CUS-000001", customer.CustomerNumber);
        Assert.Equal(
            createRequest.Email,
            customer.Email);

        HttpResponseMessage deactivateResponse =
            await SendAsync<object>(
                HttpMethod.Post,
                $"/api/customers/{customer.Id}/deactivate",
                owner.AccessToken,
                null);
        Assert.Equal(
            HttpStatusCode.NoContent,
            deactivateResponse.StatusCode);

        CustomerResponse[] activeCustomers =
            await GetListAsync(owner.AccessToken, false);
        CustomerResponse[] allCustomers =
            await GetListAsync(owner.AccessToken, true);

        Assert.DoesNotContain(
            activeCustomers,
            item => item.Id == customer.Id);
        Assert.Contains(
            allCustomers,
            item =>
                item.Id == customer.Id
                && !item.IsActive);

        HttpResponseMessage activateResponse =
            await SendAsync<object>(
                HttpMethod.Post,
                $"/api/customers/{customer.Id}/activate",
                owner.AccessToken,
                null);
        Assert.Equal(HttpStatusCode.NoContent, activateResponse.StatusCode);

        activeCustomers = await GetListAsync(owner.AccessToken, false);
        Assert.Contains(activeCustomers, item => item.Id == customer.Id);

        int storedCount =
            await _factory.ExecuteDbContextAsync(
                dbContext =>
                    dbContext.Customers.CountAsync(
                        stored =>
                            stored.Id == customer.Id));
        Assert.Equal(1, storedCount);
    }

    [Fact]
    public async Task DuplicateContact_ShouldConflictWithinTenant_ButNotAcrossTenants()
    {
        AuthenticationTokenResponse firstOwner =
            await RegisterOwnerAsync();
        AuthenticationTokenResponse secondOwner =
            await RegisterOwnerAsync();
        string email =
            $"shared-{Guid.NewGuid():N}@example.com";
        const string phone = "+905559998877";
        CustomerUpsertRequest request =
            IndividualRequest(email, phone);

        HttpResponseMessage firstResponse =
            await SendAsync(
                HttpMethod.Post,
                "/api/customers",
                firstOwner.AccessToken,
                request);
        HttpResponseMessage duplicateResponse =
            await SendAsync(
                HttpMethod.Post,
                "/api/customers",
                firstOwner.AccessToken,
                request);
        HttpResponseMessage otherTenantResponse =
            await SendAsync(
                HttpMethod.Post,
                "/api/customers",
                secondOwner.AccessToken,
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            duplicateResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.Created,
            otherTenantResponse.StatusCode);
    }

    [Fact]
    public async Task DuplicateEmailOnInactiveCustomer_ShouldSuggestReactivation()
    {
        AuthenticationTokenResponse owner = await RegisterOwnerAsync();
        string email = $"inactive-{Guid.NewGuid():N}@example.com";
        HttpResponseMessage created = await SendAsync(
            HttpMethod.Post,
            "/api/customers",
            owner.AccessToken,
            IndividualRequest(email, null));
        CustomerResponse customer = Assert.IsType<CustomerResponse>(
            await created.Content.ReadFromJsonAsync<CustomerResponse>());
        (await SendAsync<object>(
            HttpMethod.Post,
            $"/api/customers/{customer.Id}/deactivate",
            owner.AccessToken,
            null)).EnsureSuccessStatusCode();

        HttpResponseMessage duplicate = await SendAsync(
            HttpMethod.Post,
            "/api/customers",
            owner.AccessToken,
            IndividualRequest(email, null));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Contains(
            "Customer.EmailBelongsToInactiveCustomer",
            await duplicate.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Search_ShouldTreatLikeWildcardsAsLiteralCharacters()
    {
        AuthenticationTokenResponse owner = await RegisterOwnerAsync();
        (await SendAsync(
            HttpMethod.Post,
            "/api/customers",
            owner.AccessToken,
            IndividualRequest($"wildcard-{Guid.NewGuid():N}@example.com", null)))
            .EnsureSuccessStatusCode();

        HttpResponseMessage response = await SendAsync<object>(
            HttpMethod.Get,
            "/api/customers?search=%25",
            owner.AccessToken,
            null);
        response.EnsureSuccessStatusCode();
        PagedResponse<CustomerResponse> page = Assert.IsType<PagedResponse<CustomerResponse>>(
            await response.Content.ReadFromJsonAsync<PagedResponse<CustomerResponse>>());

        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task ExtremePageNumber_ShouldReturnAnEmptyPage()
    {
        AuthenticationTokenResponse owner = await RegisterOwnerAsync();

        HttpResponseMessage response = await SendAsync<object>(
            HttpMethod.Get,
            "/api/customers?page=2000000000&pageSize=100",
            owner.AccessToken,
            null);
        response.EnsureSuccessStatusCode();
        PagedResponse<CustomerResponse> page = Assert.IsType<PagedResponse<CustomerResponse>>(
            await response.Content.ReadFromJsonAsync<PagedResponse<CustomerResponse>>());

        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task CustomerId_ShouldNotLeakAcrossTenants()
    {
        AuthenticationTokenResponse firstOwner =
            await RegisterOwnerAsync();
        AuthenticationTokenResponse secondOwner =
            await RegisterOwnerAsync();

        HttpResponseMessage createResponse =
            await SendAsync(
                HttpMethod.Post,
                "/api/customers",
                firstOwner.AccessToken,
                IndividualRequest(
                    $"tenant-{Guid.NewGuid():N}@example.com",
                    null));
        createResponse.EnsureSuccessStatusCode();
        CustomerResponse? customer =
            await createResponse.Content.ReadFromJsonAsync<
                CustomerResponse>();
        Assert.NotNull(customer);

        HttpResponseMessage response =
            await SendAsync<object>(
                HttpMethod.Get,
                $"/api/customers/{customer.Id}",
                secondOwner.AccessToken,
                null);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task PrimaryAddressChange_ShouldLeaveOneActivePrimary()
    {
        AuthenticationTokenResponse owner =
            await RegisterOwnerAsync();
        HttpResponseMessage createResponse =
            await SendAsync(
                HttpMethod.Post,
                "/api/customers",
                owner.AccessToken,
                IndividualRequest(
                    $"address-{Guid.NewGuid():N}@example.com",
                    null));
        createResponse.EnsureSuccessStatusCode();
        CustomerResponse? customer =
            await createResponse.Content.ReadFromJsonAsync<
                CustomerResponse>();
        Assert.NotNull(customer);

        CustomerAddressResponse firstAddress =
            await AddAddressAsync(
                owner.AccessToken,
                customer.Id,
                "First",
                true);
        CustomerAddressResponse secondAddress =
            await AddAddressAsync(
                owner.AccessToken,
                customer.Id,
                "Second",
                false);

        HttpResponseMessage primaryResponse =
            await SendAsync<object>(
                HttpMethod.Put,
                $"/api/customers/{customer.Id}/addresses/{secondAddress.Id}/primary",
                owner.AccessToken,
                null);
        Assert.Equal(
            HttpStatusCode.OK,
            primaryResponse.StatusCode);

        HttpResponseMessage getResponse =
            await SendAsync<object>(
                HttpMethod.Get,
                $"/api/customers/{customer.Id}",
                owner.AccessToken,
                null);
        CustomerResponse? result =
            await getResponse.Content.ReadFromJsonAsync<
                CustomerResponse>();

        Assert.NotNull(result);
        Assert.Single(
            result.Addresses,
            address =>
                address.IsActive
                && address.IsPrimary
                && address.Id == secondAddress.Id);
        Assert.DoesNotContain(
            result.Addresses,
            address =>
                address.Id == firstAddress.Id
                && address.IsPrimary);
    }

    [Fact]
    public async Task OnlyActiveAddress_ShouldRemainPrimaryWhenUnchecked()
    {
        AuthenticationTokenResponse owner = await RegisterOwnerAsync();
        HttpResponseMessage created = await SendAsync(
            HttpMethod.Post,
            "/api/customers",
            owner.AccessToken,
            IndividualRequest($"only-address-{Guid.NewGuid():N}@example.com", null));
        CustomerResponse customer = Assert.IsType<CustomerResponse>(
            await created.Content.ReadFromJsonAsync<CustomerResponse>());
        CustomerAddressResponse address = await AddAddressAsync(
            owner.AccessToken,
            customer.Id,
            "Only",
            true);

        HttpResponseMessage updated = await SendAsync(
            HttpMethod.Put,
            $"/api/customers/{customer.Id}/addresses/{address.Id}",
            owner.AccessToken,
            AddressRequest("Only", false));
        updated.EnsureSuccessStatusCode();
        CustomerAddressResponse result = Assert.IsType<CustomerAddressResponse>(
            await updated.Content.ReadFromJsonAsync<CustomerAddressResponse>());

        Assert.True(result.IsPrimary);
    }

    [Fact]
    public async Task DeactivatingPrimaryAddress_ShouldPromoteOldestActiveAddress()
    {
        AuthenticationTokenResponse owner = await RegisterOwnerAsync();
        HttpResponseMessage created = await SendAsync(
            HttpMethod.Post,
            "/api/customers",
            owner.AccessToken,
            IndividualRequest($"promote-address-{Guid.NewGuid():N}@example.com", null));
        CustomerResponse customer = Assert.IsType<CustomerResponse>(
            await created.Content.ReadFromJsonAsync<CustomerResponse>());
        CustomerAddressResponse primary = await AddAddressAsync(
            owner.AccessToken, customer.Id, "Primary", true);
        CustomerAddressResponse replacement = await AddAddressAsync(
            owner.AccessToken, customer.Id, "Replacement", false);

        (await SendAsync<object>(
            HttpMethod.Post,
            $"/api/customers/{customer.Id}/addresses/{primary.Id}/deactivate",
            owner.AccessToken,
            null)).EnsureSuccessStatusCode();
        HttpResponseMessage getResponse = await SendAsync<object>(
            HttpMethod.Get,
            $"/api/customers/{customer.Id}",
            owner.AccessToken,
            null);
        CustomerResponse result = Assert.IsType<CustomerResponse>(
            await getResponse.Content.ReadFromJsonAsync<CustomerResponse>());

        Assert.Contains(result.Addresses, item =>
            item.Id == replacement.Id && item.IsActive && item.IsPrimary);
    }

    private async Task<CustomerAddressResponse>
        AddAddressAsync(
            string accessToken,
            Guid customerId,
            string label,
            bool isPrimary)
    {
        HttpResponseMessage response =
            await SendAsync(
                HttpMethod.Post,
                $"/api/customers/{customerId}/addresses",
                accessToken,
                AddressRequest(label, isPrimary));
        response.EnsureSuccessStatusCode();

        CustomerAddressResponse? address =
            await response.Content.ReadFromJsonAsync<
                CustomerAddressResponse>();

        return Assert.IsType<CustomerAddressResponse>(
            address);
    }

    private async Task<CustomerResponse[]> GetListAsync(
        string accessToken,
        bool includeInactive)
    {
        HttpResponseMessage response =
            await SendAsync<object>(
                HttpMethod.Get,
                $"/api/customers?includeInactive={includeInactive}",
                accessToken,
                null);
        response.EnsureSuccessStatusCode();

        PagedResponse<CustomerResponse>? customers =
            await response.Content.ReadFromJsonAsync<
                PagedResponse<CustomerResponse>>();

        return Assert.IsType<PagedResponse<CustomerResponse>>(
            customers).Items.ToArray();
    }

    private async Task<HttpResponseMessage> SendAsync<TBody>(
        HttpMethod method,
        string requestUri,
        string accessToken,
        TBody? body)
    {
        using HttpRequestMessage request = new(
            method,
            requestUri);
        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    private async Task<AuthenticationTokenResponse>
        RegisterOwnerAsync()
    {
        string uniqueValue = Guid.NewGuid().ToString("N");
        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest(
                    $"Customer API {uniqueValue}",
                    $"customer-api-{uniqueValue}",
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

    private static CustomerUpsertRequest IndividualRequest(
        string email,
        string? phone)
    {
        return new CustomerUpsertRequest(
            "Individual",
            "Bekir",
            "Çakmak",
            null,
            null,
            email,
            phone);
    }

    private static CustomerAddressUpsertRequest AddressRequest(
        string label,
        bool isPrimary) => new(
            label,
            $"{label} Street 1",
            null,
            "Istanbul",
            null,
            "34000",
            "TR",
            isPrimary);
}
