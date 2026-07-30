using Microsoft.AspNetCore.Identity;

using ServicePilot.Application.Abstractions.Authentication;

namespace ServicePilot.Infrastructure.Authentication;

internal sealed class AspNetPasswordHasher
    : IPasswordHasher
{
    private static readonly object UserMarker = new();

    private readonly PasswordHasher<object> _passwordHasher = new();

    public string Hash(string password)
    {
        return _passwordHasher.HashPassword(
            UserMarker,
            password);
    }

    public bool Verify(
        string passwordHash,
        string providedPassword)
    {
        PasswordVerificationResult result =
            _passwordHasher.VerifyHashedPassword(
                UserMarker,
                passwordHash,
                providedPassword);

        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}