using System.Security.Claims;
using System.Threading.RateLimiting;
using DotNetApiTemplate.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace DotNetApiTemplate.Security;

public static class AuthRateLimitPolicy
{
    public const string Register = "AuthRegister";
    public const string Login = "AuthLogin";
    public const string RefreshToken = "AuthRefreshToken";
    public const string ConfirmEmail = "AuthConfirmEmail";
    public const string ResendConfirmation = "AuthResendConfirmation";
    public const string ForgotPassword = "AuthForgotPassword";
    public const string ResetPassword = "AuthResetPassword";
    public const string ChangePassword = "AuthChangePassword";
    public const string Logout = "AuthLogout";
    public const string Me = "AuthMe";

    public static void RegisterPolicies(RateLimiterOptions options)
    {
        AddPolicy(options, Register, 5, 10);
        AddPolicy(options, Login, 10, 1);
        AddPolicy(options, RefreshToken, 30, 1);
        AddPolicy(options, ConfirmEmail, 10, 5);
        AddPolicy(options, ResendConfirmation, 3, 10);
        AddPolicy(options, ForgotPassword, 3, 10);
        AddPolicy(options, ResetPassword, 5, 10);
        AddPolicy(options, ChangePassword, 5, 10);
        AddPolicy(options, Logout, 20, 1);
        AddPolicy(options, Me, 60, 1);
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

            var key = $"auth:{policyName}:{identity}";

            return RateLimitPartition.Get(
                key,
                _ => RateLimitHelper.CreateSlidingWindowLimiter(
                    permitLimit: permitLimit,
                    windowMinutes: windowMinutes));
        });
    }
}