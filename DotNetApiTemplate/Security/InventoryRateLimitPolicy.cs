
using System.Security.Claims;
using System.Threading.RateLimiting;
using DotNetApiTemplate.Helpers;
using Microsoft.AspNetCore.RateLimiting;

namespace SupermarketERP.Security;

public static class InventoryRateLimitPolicy
{
    public const string Get = "InventoryGet";
    public const string GetById = "InventoryGetById";
    public const string GetByProduct = "InventoryGetByProduct";
    public const string GetLowStock = "InventoryGetLowStock";
    public const string Create = "InventoryCreate";
    public const string Adjust = "InventoryAdjust";
    public const string GetAdjustments = "InventoryGetAdjustments";

    public static void Register(RateLimiterOptions options)
    {
        AddPolicy(options, Get, 60);
        AddPolicy(options, GetById, 60);
        AddPolicy(options, GetByProduct, 60);
        AddPolicy(options, GetLowStock, 60);
        AddPolicy(options, Create, 10);
        AddPolicy(options, Adjust, 20);
        AddPolicy(options, GetAdjustments, 60);
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

            var key = $"inventory:{policyName}:{identity}";

            return RateLimitPartition.Get(
                key,
                _ => RateLimitHelper.CreateSlidingWindowLimiter(
                    permitLimit: permitLimit));
        });
    }
}
