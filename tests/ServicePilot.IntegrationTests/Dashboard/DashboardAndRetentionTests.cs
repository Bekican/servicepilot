using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using ServicePilot.Application.Retention;
using ServicePilot.Contracts.Authentication;
using ServicePilot.Contracts.Dashboard;
using ServicePilot.Domain.Auditing;
using ServicePilot.Domain.Users;
using ServicePilot.IntegrationTests.Infrastructure;

namespace ServicePilot.IntegrationTests.Dashboard;

[Collection(IntegrationTestCollection.Name)]
public sealed class DashboardAndRetentionTests(
    ServicePilotApiFactory factory)
{
    [Fact]
    public async Task Dashboard_ShouldReturnTenantScopedSummary()
    {
        AuthenticationTokenResponse owner =
            await RegisterOwnerAsync();
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = new(
            HttpMethod.Get,
            "/api/dashboard/summary");
        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                owner.AccessToken);

        HttpResponseMessage response =
            await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        DashboardSummaryResponse? summary =
            await response.Content.ReadFromJsonAsync<
                DashboardSummaryResponse>();
        Assert.NotNull(summary);
        Assert.Equal(1, summary.ActiveUserCount);
        Assert.Equal(0, summary.ActiveCustomerCount);
    }

    [Fact]
    public async Task Retention_ShouldDeleteOldInvitations_AndAnonymizeAudit()
    {
        AuthenticationTokenResponse owner =
            await RegisterOwnerAsync();
        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
        Guid invitationId = Guid.NewGuid();
        Guid auditLogId = Guid.NewGuid();

        await factory.ExecuteDbContextAsync(
            async dbContext =>
            {
                UserInvitation invitation = new(
                    invitationId,
                    owner.OrganizationId,
                    $"old-{Guid.NewGuid():N}@example.com",
                    UserRoles.Technician,
                    new string('a', 64),
                    nowUtc.AddDays(-60),
                    nowUtc.AddDays(-59));
                invitation.MarkAccepted(
                    nowUtc.AddDays(-59.5));
                dbContext.UserInvitations.Add(invitation);
                dbContext.AuditLogs.Add(
                    new AuditLog(
                        auditLogId,
                        owner.OrganizationId,
                        owner.UserId,
                        "User.RoleChanged",
                        nameof(User),
                        owner.UserId,
                        nowUtc.AddYears(-6),
                        "{\"old\":\"metadata\"}"));
                await dbContext.SaveChangesAsync();
                return 0;
            });

        RetentionResult result;
        await using (
            AsyncServiceScope scope =
                factory.Services.CreateAsyncScope())
        {
            IRetentionService retentionService =
                scope.ServiceProvider.GetRequiredService<
                    IRetentionService>();
            result = await retentionService.RunAsync(nowUtc);
        }

        Assert.Equal(1, result.DeletedInvitationCount);
        Assert.Equal(1, result.AnonymizedAuditLogCount);

        await factory.ExecuteDbContextAsync(
            async dbContext =>
            {
                Assert.False(
                    await dbContext.UserInvitations
                        .AnyAsync(invitation =>
                            invitation.Id == invitationId));
                AuditLog auditLog =
                    await dbContext.AuditLogs.SingleAsync(
                        log => log.Id == auditLogId);
                Assert.Null(auditLog.ActorUserId);
                Assert.Null(auditLog.Metadata);
                Assert.NotNull(auditLog.AnonymizedAtUtc);
                return 0;
            });
    }

    private async Task<AuthenticationTokenResponse>
        RegisterOwnerAsync()
    {
        string uniqueValue = Guid.NewGuid().ToString("N");
        using HttpClient client = factory.CreateClient();
        HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest(
                    $"Dashboard {uniqueValue}",
                    $"dashboard-{uniqueValue}",
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
}