using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Domain.Users;

namespace ServicePilot.UnitTests.Authentication;

internal sealed class FakeAccessTokenProvider(
    DateTimeOffset expiresAtUtc)
    : IAccessTokenProvider
{
    public AccessToken Create(User user)
    {
        return new AccessToken(
            $"token-for-{user.Id}",
            expiresAtUtc);
    }
}