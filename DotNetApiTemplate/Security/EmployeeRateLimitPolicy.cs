using System.Security.Claims;
using System.Threading.RateLimiting;
using DotNetApiTemplate.Helpers;
using Microsoft.AspNetCore.RateLimiting;

namespace SupermarketERP.Security;

public static class EmployeeRateLimitPolicy
{
    public const string Get = "EmployeeGet";
    public const string GetById = "EmployeeGetById";
    public const string Create = "EmployeeCreate";
    public const string Update = "EmployeeUpdate";
    public const string ChangeStatus = "EmployeeChangeStatus";
    public const string GetRoles = "EmployeeGetRoles";
    public const string UpdateRole = "EmployeeUpdateRole";

    public static void Register(RateLimiterOptions options)
    {
        AddPolicy(options, Get, 60);
        AddPolicy(options, GetById, 60);
        AddPolicy(options, Create, 10);
        AddPolicy(options, Update, 20);
        AddPolicy(options, ChangeStatus, 10);
        AddPolicy(options, GetRoles, 60);
        AddPolicy(options, UpdateRole, 10);
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

            var key = $"employee:{policyName}:{identity}";

            return RateLimitPartition.Get(
                key,
                _ => RateLimitHelper.CreateSlidingWindowLimiter(
                    permitLimit: permitLimit));
        });
    }
}