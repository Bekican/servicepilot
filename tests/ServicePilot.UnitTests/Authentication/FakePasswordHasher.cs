using ServicePilot.Application.Abstractions.Authentication;

namespace ServicePilot.UnitTests.Authentication;

internal sealed class FakePasswordHasher
    : IPasswordHasher
{
    public string Hash(string password)
    {
        return $"hashed::{password}";
    }

    public bool Verify(
        string passwordHash,
        string providedPassword)
    {
        return passwordHash
            == Hash(providedPassword);
    }
}