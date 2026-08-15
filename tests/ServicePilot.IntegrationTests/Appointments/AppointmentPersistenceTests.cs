using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using ServicePilot.Application.Abstractions.Persistence.Exceptions;
using ServicePilot.Application.Reminders;
using ServicePilot.Contracts.Appointments;
using ServicePilot.Contracts.Authentication;
using ServicePilot.Contracts.Customers;
using ServicePilot.Contracts.Common;
using ServicePilot.Contracts.Services;
using ServicePilot.Domain.Appointments;
using ServicePilot.Domain.Reminders;
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

    [Fact]
    public async Task Api_ShouldRequireOffset_AndTechnicianBeforeConfirmation()
    {
        AppointmentDependencies dependencies =
            await CreateDependenciesAsync();

        using HttpRequestMessage invalidRequest = new(
            HttpMethod.Post,
            "/api/appointments");
        invalidRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                dependencies.AccessToken);
        invalidRequest.Content = JsonContent.Create(
            new CreateAppointmentRequest(
                dependencies.CustomerId,
                dependencies.ServiceId,
                null,
                "2030-01-01T10:00:00",
                "2030-01-01T11:00:00"));

        HttpResponseMessage invalidResponse =
            await _client.SendAsync(invalidRequest);
        Assert.Equal(
            System.Net.HttpStatusCode.BadRequest,
            invalidResponse.StatusCode);

        using HttpRequestMessage createRequest = new(
            HttpMethod.Post,
            "/api/appointments");
        createRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                dependencies.AccessToken);
        createRequest.Content = JsonContent.Create(
            new CreateAppointmentRequest(
                dependencies.CustomerId,
                dependencies.ServiceId,
                null,
                "2030-01-01T10:00:00+03:00",
                "2030-01-01T11:00:00+03:00"));

        HttpResponseMessage createResponse =
            await _client.SendAsync(createRequest);
        Assert.Equal(
            System.Net.HttpStatusCode.Created,
            createResponse.StatusCode);
        ServicePilot.Contracts.Appointments.AppointmentResponse?
            appointment =
            await createResponse.Content.ReadFromJsonAsync<
                ServicePilot.Contracts.Appointments
                    .AppointmentResponse>();
        Assert.NotNull(appointment);

        Reminder? reminder =
            await _factory.ExecuteDbContextAsync(
                dbContext =>
                    dbContext.Reminders.SingleOrDefaultAsync(
                        candidate =>
                            candidate.OrganizationId
                                == dependencies.OrganizationId
                            && candidate.AppointmentId
                                == appointment.Id));
        Assert.NotNull(reminder);
        Assert.Equal(
            ReminderStatus.Pending,
            reminder.Status);
        Assert.Equal(
            "customer-",
            reminder.RecipientEmail![..9]);

        await _factory.ExecuteDbContextAsync(
            async dbContext =>
            {
                DateTimeOffset dueAtUtc =
                    DateTimeOffset.UtcNow.AddMinutes(-1);
                return await dbContext.Database
                    .ExecuteSqlInterpolatedAsync(
                        $"""
                        UPDATE reminders
                        SET next_attempt_at_utc = {dueAtUtc}
                        WHERE id = {reminder.Id}
                        """);
            });

        _factory.EmailSender.Clear();
        await using (
            AsyncServiceScope scope =
                _factory.Services.CreateAsyncScope())
        {
            ReminderProcessor processor =
                scope.ServiceProvider.GetRequiredService<
                    ReminderProcessor>();
            int processed =
                await processor.ProcessDueAsync();
            Assert.Equal(1, processed);
        }

        ReminderStatus deliveredStatus =
            await _factory.ExecuteDbContextAsync(
                dbContext =>
                    dbContext.Reminders
                        .Where(candidate =>
                            candidate.Id == reminder.Id)
                        .Select(candidate =>
                            candidate.Status)
                        .SingleAsync());
        Assert.Equal(
            ReminderStatus.Sent,
            deliveredStatus);
        Assert.Single(_factory.EmailSender.Messages);

        using HttpRequestMessage confirmRequest = new(
            HttpMethod.Patch,
            $"/api/appointments/{appointment.Id}/status");
        confirmRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                dependencies.AccessToken);
        confirmRequest.Content = JsonContent.Create(
            new AppointmentStatusRequest("Confirmed"));

        HttpResponseMessage confirmResponse =
            await _client.SendAsync(confirmRequest);
        Assert.Equal(
            System.Net.HttpStatusCode.BadRequest,
            confirmResponse.StatusCode);
    }

    [Fact]
    public async Task Pagination_ShouldNotDuplicateAppointmentsWithTheSameStartTime()
    {
        AppointmentDependencies dependencies = await CreateDependenciesAsync();
        DateTimeOffset start = DateTimeOffset.UtcNow.AddDays(5);
        await _factory.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.Appointments.AddRange(Enumerable.Range(0, 25).Select(_ =>
                new Appointment(
                    Guid.NewGuid(),
                    dependencies.OrganizationId,
                    dependencies.CustomerId,
                    dependencies.ServiceId,
                    null,
                    start,
                    start.AddHours(1),
                    DateTimeOffset.UtcNow)));
            await dbContext.SaveChangesAsync();
            return 0;
        });

        PagedResponse<ServicePilot.Contracts.Appointments.AppointmentResponse> first =
            await GetAppointmentPageAsync(dependencies.AccessToken, 1, 13);
        PagedResponse<ServicePilot.Contracts.Appointments.AppointmentResponse> second =
            await GetAppointmentPageAsync(dependencies.AccessToken, 2, 13);
        Guid[] ids = first.Items.Concat(second.Items).Select(item => item.Id).ToArray();

        Assert.Equal(25, ids.Length);
        Assert.Equal(25, ids.Distinct().Count());
    }

    private async Task<PagedResponse<ServicePilot.Contracts.Appointments.AppointmentResponse>>
        GetAppointmentPageAsync(string accessToken, int page, int pageSize)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Get,
            $"/api/appointments?page={page}&pageSize={pageSize}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        HttpResponseMessage response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return Assert.IsType<PagedResponse<ServicePilot.Contracts.Appointments.AppointmentResponse>>(
            await response.Content.ReadFromJsonAsync<
                PagedResponse<ServicePilot.Contracts.Appointments.AppointmentResponse>>());
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
            owner.UserId,
            owner.AccessToken);
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
        Guid TechnicianUserId,
        string AccessToken);
}
