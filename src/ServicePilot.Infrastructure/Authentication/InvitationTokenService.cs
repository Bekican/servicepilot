using System.Security.Cryptography;
using System.Text;

using ServicePilot.Application.Abstractions.Authentication;

namespace ServicePilot.Infrastructure.Authentication;

internal sealed class InvitationTokenService
    : IInvitationTokenService
{
    private const int TokenByteLength = 32;

    public InvitationToken Create()
    {
        byte[] randomBytes =
            RandomNumberGenerator.GetBytes(TokenByteLength);
        string rawToken = ToBase64Url(randomBytes);

        return new InvitationToken(
            rawToken,
            Hash(rawToken));
    }

    public string Hash(string rawToken)
    {
        byte[] tokenBytes =
            Encoding.UTF8.GetBytes(rawToken);
        byte[] hash = SHA256.HashData(tokenBytes);

        return Convert
            .ToHexString(hash)
            .ToLowerInvariant();
    }

    private static string ToBase64Url(byte[] value)
    {
        return Convert
            .ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}