using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace DotNetApiTemplate.Security;

public sealed class AuthRateLimitPolicy
    : IRateLimiterPolicy<string>
{
    public const string PolicyName = "AuthRateLimit";

    public RateLimitPartition<string> GetPartition(
        HttpContext httpContext)
    {
        var ip = GetIpAddress(httpContext);
        var fingerprint = GetFingerprint(httpContext);

        var key = $"{ip}:{fingerprint}";

        return RateLimitPartition.Get(
            key,
            _ => RateLimiter.CreateChained(
                CreateIpLimiter(),
                CreateFingerprintLimiter()));
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>?
        OnRejected
    { get; } = null;

    private static RateLimiter CreateIpLimiter()
    {
        return new SlidingWindowRateLimiter(
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    }

    private static RateLimiter CreateFingerprintLimiter()
    {
        return new SlidingWindowRateLimiter(
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    }

    private static string GetIpAddress(
        HttpContext context)
    {
        return context.Connection.RemoteIpAddress?
                   .ToString()
               ?? "unknown";
    }

    private static string GetFingerprint(
        HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(
                "X-Client-Fingerprint",
                out var fingerprint))
        {
            var value = fingerprint.ToString().Trim();

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return "no-fingerprint";
    }
}