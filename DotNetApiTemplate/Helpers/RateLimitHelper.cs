using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace DotNetApiTemplate.Helpers;

public static class RateLimitHelper
{
    public static string GetIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?
            .MapToIPv6()
            .ToString() ?? "unknown";
    }

    public static string GetFingerprint(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(
            "X-Client-Fingerprint", out var fingerprint))
        {
            var value = fingerprint.ToString().Trim();

            if (!string.IsNullOrWhiteSpace(value) &&
                value.Length <= 128)
            {
                return value;
            }
        }

        return "no-fingerprint";
    }

    public static RateLimiter CreateSlidingWindowLimiter(
        int permitLimit,
        int windowMinutes = 1,
        int segmentsPerWindow = 6)
    {
        return new SlidingWindowRateLimiter(
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(windowMinutes),
                SegmentsPerWindow = segmentsPerWindow,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    }
}