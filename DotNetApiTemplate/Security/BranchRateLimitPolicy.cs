using System.Security.Claims;
using System.Threading.RateLimiting;
using DotNetApiTemplate.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace SupermarketERP.Security;

public static class BranchRateLimitPolicy
{
    public const string Get = "BranchGet";
    public const string Create = "BranchCreate";
    public const string Update = "BranchUpdate";
    public const string Delete = "BranchDelete";
    public const string ChangeStatus = "BranchChangeStatus";
    public const string AssignManager = "BranchAssignManager";
    public const string AssignEmployee = "BranchAssignEmployee";
    public const string UnassignEmployee = "BranchUnassignEmployee";

    public static void Register(RateLimiterOptions options)
    {
        AddPolicy(options, Get, 60);
        AddPolicy(options, Create, 10);
        AddPolicy(options, Update, 20);
        AddPolicy(options, Delete, 10);
        AddPolicy(options, ChangeStatus, 10);
        AddPolicy(options, AssignManager, 10);
        AddPolicy(options, AssignEmployee, 20);
        AddPolicy(options, UnassignEmployee, 20);
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

            var key = $"branch:{policyName}:{identity}";

            return RateLimitPartition.Get(
                key,
                _ => RateLimitHelper.CreateSlidingWindowLimiter(
                    permitLimit: permitLimit));
        });
    }
}