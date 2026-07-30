using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;

using ServicePilot.Application.Abstractions.Email;
using ServicePilot.Contracts.Authentication;
using ServicePilot.Contracts.Users.Invitations;
using ServicePilot.Domain.Users;
using ServicePilot.IntegrationTests.Infrastructure;

namespace ServicePilot.IntegrationTests.Users;

[Collection(IntegrationTestCollection.Name)]
public sealed partial class UserInvitationTests
{
    private readonly HttpClient _client;
    private readonly ServicePilotApiFactory _factory;

    public UserInvitationTests(
        ServicePilotApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.EmailSender.Clear();
    }

    [Fact]
    public async Task Owner_ShouldInviteAndUserShouldAcceptOnce()
    {
        AuthenticationTokenResponse owner =
            await RegisterOwnerAsync();
        const string invitedEmail =
            "invited-admin@example.com";

        HttpResponseMessage inviteResponse =
            await InviteAsync(
                owner.AccessToken,
                invitedEmail,
                UserRoles.Admin);

        Assert.Equal(
            HttpStatusCode.Created,
            inviteResponse.StatusCode);

        UserInvitationResponse? invitationResponse =
            await inviteResponse.Content.ReadFromJsonAsync<
                UserInvitationResponse>();
        Assert.NotNull(invitationResponse);

        string rawToken = GetLatestRawToken();

        UserInvitation? storedInvitation =
            await _factory.ExecuteDbContextAsync(
                dbContext =>
                    dbContext.UserInvitations
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            invitation =>
                                invitation.Id
                                == invitationResponse.Id));

        Assert.NotNull(storedInvitation);
        Assert.Equal(64, storedInvitation.TokenHash.Length);
        Assert.DoesNotContain(
            rawToken,
            storedInvitation.TokenHash,
            StringComparison.Ordinal);

        HttpResponseMessage acceptResponse =
            await AcceptAsync(rawToken);

        Assert.Equal(
            HttpStatusCode.OK,
            acceptResponse.StatusCode);

        AuthenticationTokenResponse? authentication =
            await acceptResponse.Content.ReadFromJsonAsync<
                AuthenticationTokenResponse>();

        Assert.NotNull(authentication);
        Assert.Equal(UserRoles.Admin, authentication.Role);
        Assert.Equal(
            owner.OrganizationId,
            authentication.OrganizationId);

        HttpResponseMessage secondAcceptResponse =
            await AcceptAsync(rawToken);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            secondAcceptResponse.StatusCode);
    }

    [Fact]
    public async Task NonOwner_ShouldReceiveForbidden()
    {
        AuthenticationTokenResponse owner =
            await RegisterOwnerAsync();

        HttpResponseMessage firstInvite =
            await InviteAsync(
                owner.AccessToken,
                $"admin-{Guid.NewGuid():N}@example.com",
                UserRoles.Admin);
        firstInvite.EnsureSuccessStatusCode();

        AuthenticationTokenResponse admin =
            await AcceptAndReadAsync(
                GetLatestRawToken());

        HttpResponseMessage forbiddenResponse =
            await InviteAsync(
                admin.AccessToken,
                $"forbidden-{Guid.NewGuid():N}@example.com",
                UserRoles.Technician);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            forbiddenResponse.StatusCode);
    }

    [Fact]
    public async Task Resend_ShouldInvalidatePreviousToken()
    {
        AuthenticationTokenResponse owner =
            await RegisterOwnerAsync();

        HttpResponseMessage inviteResponse =
            await InviteAsync(
                owner.AccessToken,
                $"resend-{Guid.NewGuid():N}@example.com",
                UserRoles.Dispatcher);
        inviteResponse.EnsureSuccessStatusCode();

        UserInvitationResponse? invitation =
            await inviteResponse.Content.ReadFromJsonAsync<
                UserInvitationResponse>();
        Assert.NotNull(invitation);

        string firstToken = GetLatestRawToken();

        using HttpRequestMessage resendRequest = new(
            HttpMethod.Post,
            $"/api/users/invitations/{invitation.Id}/resend");
        resendRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                owner.AccessToken);

        HttpResponseMessage resendResponse =
            await _client.SendAsync(resendRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            resendResponse.StatusCode);

        string secondToken = GetLatestRawToken();
        Assert.NotEqual(firstToken, secondToken);

        HttpResponseMessage oldTokenResponse =
            await AcceptAsync(firstToken);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            oldTokenResponse.StatusCode);

        HttpResponseMessage newTokenResponse =
            await AcceptAsync(secondToken);
        Assert.Equal(
            HttpStatusCode.OK,
            newTokenResponse.StatusCode);
    }

    [Fact]
    public async Task Resend_ShouldReturnNotFound_ForAnotherTenant()
    {
        AuthenticationTokenResponse firstOwner =
            await RegisterOwnerAsync();
        HttpResponseMessage inviteResponse =
            await InviteAsync(
                firstOwner.AccessToken,
                $"tenant-{Guid.NewGuid():N}@example.com",
                UserRoles.Technician);
        inviteResponse.EnsureSuccessStatusCode();

        UserInvitationResponse? invitation =
            await inviteResponse.Content.ReadFromJsonAsync<
                UserInvitationResponse>();
        Assert.NotNull(invitation);

        AuthenticationTokenResponse secondOwner =
            await RegisterOwnerAsync();

        using HttpRequestMessage resendRequest = new(
            HttpMethod.Post,
            $"/api/users/invitations/{invitation.Id}/resend");
        resendRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                secondOwner.AccessToken);

        HttpResponseMessage response =
            await _client.SendAsync(resendRequest);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task ConcurrentInvitations_ShouldLeaveOnePendingRecord()
    {
        AuthenticationTokenResponse owner =
            await RegisterOwnerAsync();
        string email =
            $"concurrent-{Guid.NewGuid():N}@example.com";

        Task<HttpResponseMessage> firstTask =
            InviteAsync(
                owner.AccessToken,
                email,
                UserRoles.Technician);
        Task<HttpResponseMessage> secondTask =
            InviteAsync(
                owner.AccessToken,
                email,
                UserRoles.Technician);

        HttpResponseMessage[] responses =
            await Task.WhenAll(firstTask, secondTask);

        Assert.Contains(
            responses,
            response =>
                response.StatusCode
                == HttpStatusCode.Created);
        Assert.All(
            responses,
            response =>
                Assert.Contains(
                    response.StatusCode,
                    new[]
                    {
                        HttpStatusCode.Created,
                        HttpStatusCode.Conflict
                    }));

        int pendingCount =
            await _factory.ExecuteDbContextAsync(
                dbContext =>
                    dbContext.UserInvitations.CountAsync(
                        invitation =>
                            invitation.OrganizationId
                                == owner.OrganizationId
                            && invitation.Email == email
                            && invitation.Status
                                == UserInvitationStatus.Pending));

        Assert.Equal(1, pendingCount);
    }

    [Fact]
    public async Task Accept_ShouldRejectExpiredInvitation()
    {
        AuthenticationTokenResponse owner =
            await RegisterOwnerAsync();
        HttpResponseMessage inviteResponse =
            await InviteAsync(
                owner.AccessToken,
                $"expired-{Guid.NewGuid():N}@example.com",
                UserRoles.Technician);
        inviteResponse.EnsureSuccessStatusCode();

        UserInvitationResponse? invitation =
            await inviteResponse.Content.ReadFromJsonAsync<
                UserInvitationResponse>();
        Assert.NotNull(invitation);

        string rawToken = GetLatestRawToken();

        await _factory.ExecuteDbContextAsync(
            dbContext =>
                dbContext.UserInvitations
                    .Where(
                        stored =>
                            stored.Id == invitation.Id)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters.SetProperty(
                                stored =>
                                    stored.ExpiresAtUtc,
                                DateTimeOffset.UtcNow
                                    .AddMinutes(-1))));

        HttpResponseMessage response =
            await AcceptAsync(rawToken);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private async Task<AuthenticationTokenResponse>
        RegisterOwnerAsync()
    {
        string uniqueValue = Guid.NewGuid().ToString("N");
        RegisterRequest request = new(
            $"Organization {uniqueValue}",
            $"organization-{uniqueValue}",
            "Owner",
            "User",
            $"owner-{uniqueValue}@example.com",
            "correct-password");

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                request);
        response.EnsureSuccessStatusCode();

        AuthenticationTokenResponse? authentication =
            await response.Content.ReadFromJsonAsync<
                AuthenticationTokenResponse>();

        return Assert.IsType<
            AuthenticationTokenResponse>(
                authentication);
    }

    private async Task<HttpResponseMessage> InviteAsync(
        string accessToken,
        string email,
        string role)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Post,
            "/api/users/invitations");
        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);
        request.Content = JsonContent.Create(
            new CreateInvitationRequest(email, role));

        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> AcceptAsync(
        string rawToken)
    {
        return await _client.PostAsJsonAsync(
            "/api/auth/invitations/accept",
            new AcceptInvitationRequest(
                rawToken,
                "Invited",
                "User",
                "invited-password"));
    }

    private async Task<AuthenticationTokenResponse>
        AcceptAndReadAsync(string rawToken)
    {
        HttpResponseMessage response =
            await AcceptAsync(rawToken);
        response.EnsureSuccessStatusCode();

        AuthenticationTokenResponse? authentication =
            await response.Content.ReadFromJsonAsync<
                AuthenticationTokenResponse>();

        return Assert.IsType<
            AuthenticationTokenResponse>(
                authentication);
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
}