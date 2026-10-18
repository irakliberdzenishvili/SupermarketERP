using System.Threading.RateLimiting;
using DotNetApiTemplate.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace DotNetApiTemplate.Security;

public sealed class AuthRateLimitPolicy : IRateLimiterPolicy<string>
{
    public const string PolicyName = "AuthRateLimit";

    public RateLimitPartition<string> GetPartition(
        HttpContext httpContext)
    {
        var ip = RateLimitHelper.GetIpAddress(httpContext);

        return RateLimitPartition.Get(
            $"auth:{ip}",
            _ => RateLimitHelper.CreateSlidingWindowLimiter(
                permitLimit: 10));
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>?
        OnRejected
    { get; } = null;
}