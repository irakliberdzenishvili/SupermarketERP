
using System.Security.Claims;
using System.Threading.RateLimiting;
using DotNetApiTemplate.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace SupermarketERP.Security;

public static class SalesRateLimitPolicy
{
    public const string Get = "SalesGet";
    public const string GetById = "SalesGetById";
    public const string Create = "SalesCreate";
    public const string AddItem = "SalesAddItem";
    public const string UpdateItem = "SalesUpdateItem";
    public const string DeleteItem = "SalesDeleteItem";
    public const string Payment = "SalesPayment";
    public const string Cancel = "SalesCancel";
    public const string Receipt = "SalesReceipt";

    public static void Register(RateLimiterOptions options)
    {
        AddPolicy(options, Get, 60);
        AddPolicy(options, GetById, 60);
        AddPolicy(options, Create, 10);
        AddPolicy(options, AddItem, 20);
        AddPolicy(options, UpdateItem, 20);
        AddPolicy(options, DeleteItem, 10);
        AddPolicy(options, Payment, 10);
        AddPolicy(options, Cancel, 10);
        AddPolicy(options, Receipt, 60);
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

            var key = $"sales:{policyName}:{identity}";

            return RateLimitPartition.Get(
                key,
                _ => RateLimitHelper.CreateSlidingWindowLimiter(
                    permitLimit: permitLimit));
        });
    }
}
