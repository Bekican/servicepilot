using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;

namespace ServicePilot.Api.Authentication;

public sealed class PasswordResetRequestLimiter : IAsyncDisposable
{
    private readonly PartitionedRateLimiter<string> _limiter;

    public PasswordResetRequestLimiter(IConfiguration configuration)
    {
        int permitLimit = configuration.GetValue(
            "RateLimiting:PasswordResetPermitLimit",
            3);
        int windowMinutes = configuration.GetValue(
            "RateLimiting:PasswordResetWindowMinutes",
            15);
        _limiter = PartitionedRateLimiter.Create<string, string>(key =>
            RateLimitPartition.GetFixedWindowLimiter(
                key,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = Math.Clamp(permitLimit, 1, 20),
                    Window = TimeSpan.FromMinutes(Math.Clamp(windowMinutes, 1, 60)),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    }

    public ValueTask<RateLimitLease> AcquireAsync(
        string organizationSlug,
        string email,
        CancellationToken cancellationToken)
    {
        string normalized = $"{organizationSlug?.Trim().ToLowerInvariant()}\n"
            + email?.Trim().ToLowerInvariant();
        string partition = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        return _limiter.AcquireAsync(partition, 1, cancellationToken);
    }

    public ValueTask DisposeAsync() => _limiter.DisposeAsync();
}
