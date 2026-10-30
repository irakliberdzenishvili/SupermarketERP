
using System.Security.Claims;
using System.Threading.RateLimiting;
using DotNetApiTemplate.Helpers;
using Microsoft.AspNetCore.RateLimiting;

namespace SupermarketERP.Security;

public static class ProductRateLimitPolicy
{
    public const string Get = "ProductGet";
    public const string GetById = "ProductGetById";
    public const string GetByBarcode = "ProductGetByBarcode";
    public const string Create = "ProductCreate";
    public const string Update = "ProductUpdate";
    public const string ChangeStatus = "ProductChangeStatus";
    public const string Delete = "ProductDelete";

    public static void Register(RateLimiterOptions options)
    {
        AddPolicy(options, Get, 60);
        AddPolicy(options, GetById, 60);
        AddPolicy(options, GetByBarcode, 60);
        AddPolicy(options, Create, 10);
        AddPolicy(options, Update, 20);
        AddPolicy(options, ChangeStatus, 10);
        AddPolicy(options, Delete, 10);
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

            var key = $"product:{policyName}:{identity}";

            return RateLimitPartition.Get(
                key,
                _ => RateLimitHelper.CreateSlidingWindowLimiter(
                    permitLimit: permitLimit));
        });
    }
}
