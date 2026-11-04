using System.Security.Claims;
using System.Threading.RateLimiting;
using DotNetApiTemplate.Helpers;
using Microsoft.AspNetCore.RateLimiting;

namespace DotNetApiTemplate.Security;

public static class StockRateLimitPolicy
{
    public const string GetStockTransfers = "GetStockTransfers";
    public const string GetStockTransferById = "GetStockTransferById";
    public const string CreateStockTransfer = "CreateStockTransfer";
    public const string ApproveStockTransfer = "ApproveStockTransfer";
    public const string RejectStockTransfer = "RejectStockTransfer";
    public const string DispatchStockTransfer = "DispatchStockTransfer";
    public const string ReceiveStockTransfer = "ReceiveStockTransfer";
    public const string CancelStockTransfer = "CancelStockTransfer";

    public static void Register(RateLimiterOptions options)
    {
        AddPolicy(options, GetStockTransfers, 60, 1);
        AddPolicy(options, GetStockTransferById, 60, 1);
        AddPolicy(options, CreateStockTransfer, 10, 1);
        AddPolicy(options, ApproveStockTransfer, 10, 1);
        AddPolicy(options, RejectStockTransfer, 10, 1);
        AddPolicy(options, DispatchStockTransfer, 10, 1);
        AddPolicy(options, ReceiveStockTransfer, 10, 1);
        AddPolicy(options, CancelStockTransfer, 10, 1);
    }

    private static void AddPolicy(
        RateLimiterOptions options,
        string policyName,
        int permitLimit,
        int windowMinutes)
    {
        options.AddPolicy(policyName, context =>
        {
            var userId = context.User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            var identity = !string.IsNullOrWhiteSpace(userId)
                ? $"user:{userId}"
                : $"ip:{RateLimitHelper.GetIpAddress(context)}";

            var key = $"stock:{policyName}:{identity}";

            return RateLimitPartition.Get(
                key,
                _ => RateLimitHelper.CreateSlidingWindowLimiter(
                    permitLimit: permitLimit,
                    windowMinutes: windowMinutes));
        });
    }
}