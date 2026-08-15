using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;

using ServicePilot.Contracts.Authentication;
using ServicePilot.Domain.Users;
using ServicePilot.IntegrationTests.Infrastructure;

namespace ServicePilot.IntegrationTests.Authentication;

[Collection(IntegrationTestCollection.Name)]
public sealed class AuthenticationTests
{
    private readonly HttpClient _client;
    private readonly ServicePilotApiFactory _factory;

    public AuthenticationTests(
        ServicePilotApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ShouldCreateOrganizationAndOwner()
    {
        RegisterRequest request = CreateRegisterRequest();

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        AuthenticationTokenResponse? authentication =
            await response.Content.ReadFromJsonAsync<
                AuthenticationTokenResponse>();

        Assert.NotNull(authentication);
        Assert.Equal(
            UserRoles.Owner,
            authentication.Role);
        Assert.False(
            string.IsNullOrWhiteSpace(
                authentication.AccessToken));

        int organizationCount =
            await _factory.ExecuteDbContextAsync(
                dbContext =>
                    dbContext.Organizations.CountAsync(
                        organization =>
                            organization.Id
                            == authentication.OrganizationId
                            && organization.Slug
                            == request.OrganizationSlug));

        User? owner =
            await _factory.ExecuteDbContextAsync(
                dbContext =>
                    dbContext.Users
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            user =>
                                user.Id
                                == authentication.UserId));

        Assert.Equal(1, organizationCount);
        Assert.NotNull(owner);
        Assert.Equal(
            authentication.OrganizationId,
            owner.OrganizationId);
        Assert.Equal(
            request.Email,
            owner.Email);
        Assert.NotEqual(
            request.Password,
            owner.PasswordHash);
    }

    [Fact]
    public async Task Login_ShouldReturnToken_WhenCredentialsAreValid()
    {
        RegisterRequest registration =
            CreateRegisterRequest();

        HttpResponseMessage registerResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                registration);

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        LoginRequest login = new(
            registration.OrganizationSlug.ToUpperInvariant(),
            registration.Email.ToUpperInvariant(),
            registration.Password);

        HttpResponseMessage loginResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                login);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        AuthenticationTokenResponse? authentication =
            await loginResponse.Content.ReadFromJsonAsync<
                AuthenticationTokenResponse>();

        Assert.NotNull(authentication);
        Assert.False(
            string.IsNullOrWhiteSpace(
                authentication.AccessToken));
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenPasswordIsWrong()
    {
        RegisterRequest registration =
            CreateRegisterRequest();

        HttpResponseMessage registerResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                registration);

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        LoginRequest login = new(
            registration.OrganizationSlug,
            registration.Email,
            "wrong-password");

        HttpResponseMessage loginResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                login);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            loginResponse.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldAllowSameEmail_InDifferentOrganizations()
    {
        RegisterRequest firstRequest =
            CreateRegisterRequest();

        RegisterRequest secondRequest =
            CreateRegisterRequest() with
            {
                Email = firstRequest.Email
            };

        HttpResponseMessage firstResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                firstRequest);

        HttpResponseMessage secondResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                secondRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.Created,
            secondResponse.StatusCode);

        int userCount =
            await _factory.ExecuteDbContextAsync(
                dbContext =>
                    dbContext.Users.CountAsync(
                        user =>
                            user.Email
                            == firstRequest.Email));

        Assert.Equal(2, userCount);
    }

    [Fact]
    public async Task Register_ShouldCreateOneOrganizationAndOwner_WhenRequestsAreConcurrent()
    {
        RegisterRequest firstRequest =
            CreateRegisterRequest();

        RegisterRequest secondRequest =
            CreateRegisterRequest() with
            {
                OrganizationSlug =
                    firstRequest.OrganizationSlug
            };

        Task<HttpResponseMessage> firstTask =
            _client.PostAsJsonAsync(
                "/api/auth/register",
                firstRequest);

        Task<HttpResponseMessage> secondTask =
            _client.PostAsJsonAsync(
                "/api/auth/register",
                secondRequest);

        HttpResponseMessage[] responses =
            await Task.WhenAll(
                firstTask,
                secondTask);

        Assert.Single(
            responses,
            response =>
                response.StatusCode
                == HttpStatusCode.Created);

        Assert.Single(
            responses,
            response =>
                response.StatusCode
                == HttpStatusCode.Conflict);

        (int OrganizationCount, int OwnerCount) state =
            await _factory.ExecuteDbContextAsync(
                async dbContext =>
                {
                    Guid[] organizationIds =
                        await dbContext.Organizations
                            .Where(
                                organization =>
                                    organization.Slug
                                    == firstRequest.OrganizationSlug)
                            .Select(
                                organization =>
                                    organization.Id)
                            .ToArrayAsync();

                    int ownerCount =
                        await dbContext.Users.CountAsync(
                            user =>
                                organizationIds.Contains(
                                    user.OrganizationId));

                    return (
                        organizationIds.Length,
                        ownerCount);
                });

        Assert.Equal(1, state.OrganizationCount);
        Assert.Equal(1, state.OwnerCount);
    }

    [Fact]
    public async Task Me_ShouldReturnTenantClaims_WhenTokenIsValid()
    {
        AuthenticationTokenResponse authentication =
            await RegisterAsync();

        using HttpRequestMessage request = new(
            HttpMethod.Get,
            "/api/auth/me");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                authentication.AccessToken);

        HttpResponseMessage response =
            await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        CurrentUserResponse? currentUser =
            await response.Content.ReadFromJsonAsync<
                CurrentUserResponse>();

        Assert.NotNull(currentUser);
        Assert.Equal(
            authentication.UserId,
            currentUser.UserId);
        Assert.Equal(
            authentication.OrganizationId,
            currentUser.OrganizationId);
        Assert.Equal(
            UserRoles.Owner,
            currentUser.Role);
    }

    [Fact]
    public async Task Me_ShouldReturnUnauthorized_WhenTokenIsMissing()
    {
        HttpResponseMessage response =
            await _client.GetAsync("/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task PasswordReset_ShouldChangePassword_InvalidateOldSession_AndRejectReuse()
    {
        _factory.EmailSender.Clear();
        RegisterRequest registration = CreateRegisterRequest();
        HttpResponseMessage registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            registration);
        registerResponse.EnsureSuccessStatusCode();
        AuthenticationTokenResponse authentication = Assert.IsType<AuthenticationTokenResponse>(
            await registerResponse.Content.ReadFromJsonAsync<AuthenticationTokenResponse>());

        HttpResponseMessage requestResponse = await _client.PostAsJsonAsync(
            "/api/auth/password-reset/request",
            new PasswordResetRequest(
                registration.OrganizationSlug,
                registration.Email));
        Assert.Equal(HttpStatusCode.Accepted, requestResponse.StatusCode);

        string body = Assert.Single(_factory.EmailSender.Messages).TextBody;
        string link = Assert.Single(
            body.Split(Environment.NewLine),
            line => line.StartsWith("https://", StringComparison.Ordinal));
        string token = new Uri(link).Query["?token=".Length..];
        const string newPassword = "new-correct-password";

        HttpResponseMessage completeResponse = await _client.PostAsJsonAsync(
            "/api/auth/password-reset/complete",
            new CompletePasswordResetRequest(token, newPassword));
        Assert.Equal(HttpStatusCode.NoContent, completeResponse.StatusCode);

        using HttpRequestMessage oldSessionRequest = new(HttpMethod.Get, "/api/auth/me");
        oldSessionRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            authentication.AccessToken);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _client.SendAsync(oldSessionRequest)).StatusCode);

        HttpResponseMessage oldPasswordLogin = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(
                registration.OrganizationSlug,
                registration.Email,
                registration.Password));
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordLogin.StatusCode);

        HttpResponseMessage newPasswordLogin = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(
                registration.OrganizationSlug,
                registration.Email,
                newPassword));
        Assert.Equal(HttpStatusCode.OK, newPasswordLogin.StatusCode);

        HttpResponseMessage reusedToken = await _client.PostAsJsonAsync(
            "/api/auth/password-reset/complete",
            new CompletePasswordResetRequest(token, "another-password"));
        Assert.Equal(HttpStatusCode.BadRequest, reusedToken.StatusCode);
    }

    [Fact]
    public async Task PasswordResetRequest_ShouldNotRevealWhetherAccountExists()
    {
        _factory.EmailSender.Clear();
        RegisterRequest registration = CreateRegisterRequest();
        (await _client.PostAsJsonAsync("/api/auth/register", registration))
            .EnsureSuccessStatusCode();

        HttpResponseMessage existing = await _client.PostAsJsonAsync(
            "/api/auth/password-reset/request",
            new PasswordResetRequest(
                registration.OrganizationSlug,
                registration.Email));
        HttpResponseMessage missing = await _client.PostAsJsonAsync(
            "/api/auth/password-reset/request",
            new PasswordResetRequest(
                registration.OrganizationSlug,
                $"missing-{Guid.NewGuid():N}@example.com"));

        Assert.Equal(HttpStatusCode.Accepted, existing.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, missing.StatusCode);
    }

    private async Task<AuthenticationTokenResponse> RegisterAsync()
    {
        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                CreateRegisterRequest());

        response.EnsureSuccessStatusCode();

        AuthenticationTokenResponse? authentication =
            await response.Content.ReadFromJsonAsync<
                AuthenticationTokenResponse>();

        return Assert.IsType<
            AuthenticationTokenResponse>(
                authentication);
    }

    private static RegisterRequest CreateRegisterRequest()
    {
        string uniqueValue = Guid.NewGuid().ToString("N");

        return new RegisterRequest(
            $"Organization {uniqueValue}",
            $"organization-{uniqueValue}",
            "Bekir",
            "Çakmak",
            $"bekir-{uniqueValue}@example.com",
            "correct-password");
    }
}
