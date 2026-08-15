using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;

using ServicePilot.Application.Abstractions.Email;
using ServicePilot.Application.Abstractions.Persistence.Exceptions;
using ServicePilot.Contracts.Authentication;
using ServicePilot.Contracts.Users;
using ServicePilot.Contracts.Users.Invitations;
using ServicePilot.Domain.Auditing;
using ServicePilot.Domain.Users;
using ServicePilot.IntegrationTests.Infrastructure;

using ContractUserResponse =
    ServicePilot.Contracts.Users.UserResponse;

namespace ServicePilot.IntegrationTests.Users;

[Collection(IntegrationTestCollection.Name)]
public sealed partial class UserManagementTests
{
    private const string InvitedPassword =
        "invited-password";

    private readonly HttpClient _client;
    private readonly ServicePilotApiFactory _factory;

    public UserManagementTests(
        ServicePilotApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.EmailSender.Clear();
    }

    [Fact]
    public async Task List_ShouldReturnOnlyCurrentTenantUsers()
    {
        RegisteredOwner firstOwner =
            await RegisterOwnerAsync();
        AuthenticationTokenResponse admin =
            await InviteAndAcceptAsync(
                firstOwner.Authentication,
                UserRoles.Admin);
        RegisteredOwner otherTenantOwner =
            await RegisterOwnerAsync();

        using HttpRequestMessage request = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/users",
            firstOwner.Authentication.AccessToken);

        HttpResponseMessage response =
            await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ContractUserResponse[]? users =
            await response.Content.ReadFromJsonAsync<
                ContractUserResponse[]>();

        Assert.NotNull(users);
        Assert.Equal(2, users.Length);
        Assert.Contains(
            users,
            user =>
                user.Id
                == firstOwner.Authentication.UserId);
        Assert.Contains(
            users,
            user => user.Id == admin.UserId);
        Assert.DoesNotContain(
            users,
            user =>
                user.Id
                == otherTenantOwner.Authentication.UserId);
    }

    [Fact]
    public async Task RoleAndStatusChanges_ShouldBeAudited_AndBlockLogin()
    {
        RegisteredOwner owner =
            await RegisterOwnerAsync();
        string invitedEmail =
            $"technician-{Guid.NewGuid():N}@example.com";
        AuthenticationTokenResponse technician =
            await InviteAndAcceptAsync(
                owner.Authentication,
                UserRoles.Technician,
                invitedEmail);

        HttpResponseMessage roleResponse =
            await PatchAsync(
                $"/api/users/{technician.UserId}/role",
                owner.Authentication.AccessToken,
                new ChangeUserRoleRequest(
                    UserRoles.Dispatcher));
        Assert.Equal(HttpStatusCode.OK, roleResponse.StatusCode);

        HttpResponseMessage statusResponse =
            await PatchAsync(
                $"/api/users/{technician.UserId}/status",
                owner.Authentication.AccessToken,
                new ChangeUserStatusRequest(false));
        Assert.Equal(
            HttpStatusCode.OK,
            statusResponse.StatusCode);

        HttpResponseMessage loginResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    owner.Registration.OrganizationSlug,
                    invitedEmail,
                    InvitedPassword));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            loginResponse.StatusCode);

        AuditLog[] auditLogs =
            await _factory.ExecuteDbContextAsync(
                dbContext =>
                    dbContext.AuditLogs
                        .AsNoTracking()
                        .Where(auditLog =>
                            auditLog.EntityId
                                == technician.UserId
                            && (
                                auditLog.Action
                                    == AuditLogActions.RoleChanged
                                || auditLog.Action
                                    == AuditLogActions.Deactivated
                            ))
                        .OrderBy(auditLog =>
                            auditLog.OccurredAtUtc)
                        .ToArrayAsync());

        Assert.Equal(2, auditLogs.Length);
        Assert.All(
            auditLogs,
            auditLog =>
                Assert.Equal(
                    owner.Authentication.UserId,
                    auditLog.ActorUserId));
        Assert.DoesNotContain(
            auditLogs,
            auditLog =>
                auditLog.Metadata?.Contains(
                    invitedEmail,
                    StringComparison.OrdinalIgnoreCase)
                == true);
    }

    [Fact]
    public async Task Owner_ShouldNotModifyOwnRoleOrStatus()
    {
        RegisteredOwner owner =
            await RegisterOwnerAsync();

        HttpResponseMessage roleResponse =
            await PatchAsync(
                $"/api/users/{owner.Authentication.UserId}/role",
                owner.Authentication.AccessToken,
                new ChangeUserRoleRequest(UserRoles.Admin));
        HttpResponseMessage statusResponse =
            await PatchAsync(
                $"/api/users/{owner.Authentication.UserId}/status",
                owner.Authentication.AccessToken,
                new ChangeUserStatusRequest(false));

        Assert.Equal(
            HttpStatusCode.Conflict,
            roleResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            statusResponse.StatusCode);
    }

    [Fact]
    public async Task InactiveOwnerToken_ShouldBeRejectedAsUnauthorized()
    {
        RegisteredOwner firstOwner =
            await RegisterOwnerAsync();
        AuthenticationTokenResponse secondOwner =
            await InviteAndAcceptAsync(
                firstOwner.Authentication,
                UserRoles.Owner);

        HttpResponseMessage deactivateResponse =
            await PatchAsync(
                $"/api/users/{secondOwner.UserId}/status",
                firstOwner.Authentication.AccessToken,
                new ChangeUserStatusRequest(false));
        Assert.Equal(
            HttpStatusCode.OK,
            deactivateResponse.StatusCode);

        using HttpRequestMessage listRequest =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/users",
                secondOwner.AccessToken);

        HttpResponseMessage response =
            await _client.SendAsync(listRequest);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Change_ShouldReturnNotFound_ForAnotherTenant()
    {
        RegisteredOwner firstOwner =
            await RegisterOwnerAsync();
        RegisteredOwner secondOwner =
            await RegisterOwnerAsync();

        HttpResponseMessage response =
            await PatchAsync(
                $"/api/users/{secondOwner.Authentication.UserId}/role",
                firstOwner.Authentication.AccessToken,
                new ChangeUserRoleRequest(UserRoles.Admin));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task ConcurrentDemotions_ShouldKeepOneActiveOwner()
    {
        RegisteredOwner firstOwner =
            await RegisterOwnerAsync();
        AuthenticationTokenResponse secondOwner =
            await InviteAndAcceptAsync(
                firstOwner.Authentication,
                UserRoles.Owner);

        TaskCompletionSource firstReady = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondReady = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Task<bool> firstDemotion = DemoteDirectAsync(
            firstOwner.Authentication.UserId,
            firstReady,
            secondReady.Task);
        Task<bool> secondDemotion = DemoteDirectAsync(
            secondOwner.UserId,
            secondReady,
            firstReady.Task);

        bool[] results = await Task.WhenAll(
            firstDemotion,
            secondDemotion);

        Assert.Single(results, succeeded => succeeded);
        Assert.Single(results, succeeded => !succeeded);

        int activeOwnerCount =
            await _factory.ExecuteDbContextAsync(
                dbContext =>
                    dbContext.Users.CountAsync(
                        user =>
                            user.OrganizationId
                                == firstOwner
                                    .Authentication
                                    .OrganizationId
                            && user.Role == UserRoles.Owner
                            && user.IsActive));

        Assert.Equal(1, activeOwnerCount);
    }

    private async Task<bool> DemoteDirectAsync(
        Guid userId,
        TaskCompletionSource ready,
        Task otherReady)
    {
        return await _factory.ExecuteDbContextAsync(
            async dbContext =>
            {
                User user = await dbContext.Users.SingleAsync(
                    candidate => candidate.Id == userId);
                user.ChangeRole(UserRoles.Admin);

                ready.SetResult();
                await otherReady;

                try
                {
                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (ConstraintViolationException exception)
                    when (
                        exception.ConstraintName
                        == "ck_users_last_active_owner")
                {
                    return false;
                }
            });
    }

    private async Task<RegisteredOwner> RegisterOwnerAsync()
    {
        string uniqueValue = Guid.NewGuid().ToString("N");
        RegisterRequest registration = new(
            $"Organization {uniqueValue}",
            $"organization-{uniqueValue}",
            "Owner",
            "User",
            $"owner-{uniqueValue}@example.com",
            "correct-password");

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                registration);
        response.EnsureSuccessStatusCode();

        AuthenticationTokenResponse? authentication =
            await response.Content.ReadFromJsonAsync<
                AuthenticationTokenResponse>();

        return new RegisteredOwner(
            registration,
            Assert.IsType<
                AuthenticationTokenResponse>(
                    authentication));
    }

    private async Task<AuthenticationTokenResponse>
        InviteAndAcceptAsync(
            AuthenticationTokenResponse owner,
            string role,
            string? email = null)
    {
        email ??=
            $"invited-{Guid.NewGuid():N}@example.com";

        using HttpRequestMessage invitationRequest =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/users/invitations",
                owner.AccessToken);
        invitationRequest.Content = JsonContent.Create(
            new CreateInvitationRequest(email, role));

        HttpResponseMessage invitationResponse =
            await _client.SendAsync(invitationRequest);
        invitationResponse.EnsureSuccessStatusCode();

        string rawToken = GetLatestRawToken();
        HttpResponseMessage acceptResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/invitations/accept",
                new AcceptInvitationRequest(
                    rawToken,
                    "Invited",
                    "User",
                    InvitedPassword));
        acceptResponse.EnsureSuccessStatusCode();

        AuthenticationTokenResponse? authentication =
            await acceptResponse.Content.ReadFromJsonAsync<
                AuthenticationTokenResponse>();

        return Assert.IsType<
            AuthenticationTokenResponse>(
                authentication);
    }

    private async Task<HttpResponseMessage> PatchAsync<TBody>(
        string requestUri,
        string accessToken,
        TBody body)
    {
        using HttpRequestMessage request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                requestUri,
                accessToken);
        request.Content = JsonContent.Create(body);

        return await _client.SendAsync(request);
    }

    private static HttpRequestMessage
        CreateAuthorizedRequest(
            HttpMethod method,
            string requestUri,
            string accessToken)
    {
        HttpRequestMessage request = new(
            method,
            requestUri);
        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        return request;
    }

    private string GetLatestRawToken()
    {
        EmailMessage message =
            Assert.Single(
                _factory.EmailSender.Messages
                    .TakeLast(1));
        Match match =
            InvitationTokenRegex().Match(
                message.TextBody);

        Assert.True(match.Success);
        return Uri.UnescapeDataString(
            match.Groups["token"].Value);
    }

    [GeneratedRegex(
        @"[?&]token=(?<token>[^\r\n]+)",
        RegexOptions.CultureInvariant)]
    private static partial Regex InvitationTokenRegex();

    private sealed record RegisteredOwner(
        RegisterRequest Registration,
        AuthenticationTokenResponse Authentication);
}
