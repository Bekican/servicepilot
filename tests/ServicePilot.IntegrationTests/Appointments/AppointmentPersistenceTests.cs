using System.Net.Http.Headers;
using System.Net.Http.Json;

using ServicePilot.Application.Abstractions.Persistence.Exceptions;
using ServicePilot.Contracts.Authentication;
using ServicePilot.Contracts.Customers;
using ServicePilot.Contracts.Services;
using ServicePilot.Domain.Appointments;
using ServicePilot.IntegrationTests.Infrastructure;

namespace ServicePilot.IntegrationTests.Appointments;

[Collection(IntegrationTestCollection.Name)]
public sealed class AppointmentPersistenceTests
{
    private readonly HttpClient _client;
    private readonly ServicePilotApiFactory _factory;

    public AppointmentPersistenceTests(
        ServicePilotApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task OverlapConstraint_ShouldRejectSameTechnician()
    {
        AppointmentDependencies dependencies =
            await CreateDependenciesAsync();
        DateTimeOffset start =
            DateTimeOffset.UtcNow.AddDays(2);

        await _factory.ExecuteDbContextAsync(
            async dbContext =>
            {
                dbContext.Appointments.Add(
                    CreateAppointment(
                        dependencies,
                        start,
                        start.AddHours(1)));
                await dbContext.SaveChangesAsync();
                return 0;
            });

        ConstraintViolationException exception =
            await Assert.ThrowsAsync<
                ConstraintViolationException>(() =>
                    _factory.ExecuteDbContextAsync(
                        async dbContext =>
                        {
                            dbContext.Appointments.Add(
                                CreateAppointment(
                                    dependencies,
                                    start.AddMinutes(30),
                                    start.AddHours(2)));
                            await dbContext.SaveChangesAsync();
                            return 0;
                        }));

        Assert.Equal(
            "ex_appointments_technician_overlap",
            exception.ConstraintName);
    }

    private static Appointment CreateAppointment(
        AppointmentDependencies dependencies,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        return new Appointment(
            Guid.NewGuid(),
            dependencies.OrganizationId,
            dependencies.CustomerId,
            dependencies.ServiceId,
            dependencies.TechnicianUserId,
            start,
            end,
            DateTimeOffset.UtcNow);
    }

    private async Task<AppointmentDependencies>
        CreateDependenciesAsync()
    {
        AuthenticationTokenResponse owner =
            await RegisterOwnerAsync();

        using HttpRequestMessage customerRequest = new(
            HttpMethod.Post,
            "/api/customers");
        customerRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                owner.AccessToken);
        customerRequest.Content = JsonContent.Create(
            new CustomerUpsertRequest(
                "Individual",
                "Test",
                "Customer",
                null,
                null,
                $"customer-{Guid.NewGuid():N}@example.com",
                null));
        HttpResponseMessage customerResponse =
            await _client.SendAsync(customerRequest);
        customerResponse.EnsureSuccessStatusCode();
        CustomerResponse? customer =
            await customerResponse.Content.ReadFromJsonAsync<
                CustomerResponse>();
        Assert.NotNull(customer);

        using HttpRequestMessage serviceRequest = new(
            HttpMethod.Post,
            "/api/services");
        serviceRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                owner.AccessToken);
        serviceRequest.Content = JsonContent.Create(
            new ServiceUpsertRequest("Repair", 60));
        HttpResponseMessage serviceResponse =
            await _client.SendAsync(serviceRequest);
        serviceResponse.EnsureSuccessStatusCode();
        ServiceResponse? service =
            await serviceResponse.Content.ReadFromJsonAsync<
                ServiceResponse>();
        Assert.NotNull(service);

        return new AppointmentDependencies(
            owner.OrganizationId,
            customer.Id,
            service.Id,
            owner.UserId);
    }

    private async Task<AuthenticationTokenResponse>
        RegisterOwnerAsync()
    {
        string uniqueValue = Guid.NewGuid().ToString("N");
        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest(
                    $"Appointment Core {uniqueValue}",
                    $"appointment-core-{uniqueValue}",
                    "Owner",
                    "User",
                    $"owner-{uniqueValue}@example.com",
                    "correct-password"));
        response.EnsureSuccessStatusCode();

        AuthenticationTokenResponse? owner =
            await response.Content.ReadFromJsonAsync<
                AuthenticationTokenResponse>();

        return Assert.IsType<
            AuthenticationTokenResponse>(owner);
    }

    private sealed record AppointmentDependencies(
        Guid OrganizationId,
        Guid CustomerId,
        Guid ServiceId,
        Guid TechnicianUserId);
}