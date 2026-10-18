using System.Threading.RateLimiting;
using DotNetApiTemplate.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace SupermarketERP.Security;

public sealed class BranchRateLimitPolicy : IRateLimiterPolicy<string>
{
    public const string PolicyName = "BranchRateLimit";

    public RateLimitPartition<string> GetPartition(
        HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var key = !string.IsNullOrWhiteSpace(userId)
            ? $"branch:user:{userId}"
            : $"branch:ip:{RateLimitHelper.GetIpAddress(httpContext)}";

        return RateLimitPartition.Get(
            key,
            _ => RateLimitHelper.CreateSlidingWindowLimiter(
                permitLimit: 60));
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>?
        OnRejected
    { get; } = null;
}