using System.Security.Claims;
using System.Threading.RateLimiting;
using DotNetApiTemplate.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace SupermarketERP.Security;

public static class WarehouseRateLimitPolicy
{
    public const string Get = "WarehouseGet";
    public const string Create = "WarehouseCreate";
    public const string Update = "WarehouseUpdate";
    public const string Delete = "WarehouseDelete";
    public const string AssignManager = "WarehouseAssignManager";

    public static void Register(RateLimiterOptions options)
    {
        AddPolicy(options, Get, 60);
        AddPolicy(options, Create, 10);
        AddPolicy(options, Update, 20);
        AddPolicy(options, Delete, 10);
        AddPolicy(options, AssignManager, 10);
    }

    private static void AddPolicy(
        RateLimiterOptions options,
        string policyName,
        int permitLimit)
    {
        options.AddPolicy(policyName, context =>
        {
            var userId = context.User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            var identity = !string.IsNullOrWhiteSpace(userId)
                ? $"user:{userId}"
                : $"ip:{RateLimitHelper.GetIpAddress(context)}";

            var key = $"warehouse:{policyName}:{identity}";

            return RateLimitPartition.Get(
                key,
                _ => RateLimitHelper.CreateSlidingWindowLimiter(
                    permitLimit: permitLimit));
        });
    }
}