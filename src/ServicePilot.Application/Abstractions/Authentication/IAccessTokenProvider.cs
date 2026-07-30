using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Abstractions.Authentication;

public interface IAccessTokenProvider
{
    AccessToken Create(User user);
}