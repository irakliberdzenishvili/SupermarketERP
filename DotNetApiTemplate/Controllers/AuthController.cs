
using System.Security.Claims;
using DotNetApiTemplate.Interfaces;
using DotNetApiTemplate.Models.Request.Auth;
using DotNetApiTemplate.Models.Response;
using DotNetApiTemplate.Models.Response.Auth;
using DotNetApiTemplate.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DotNetApiTemplate.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting(AuthRateLimitPolicy.PolicyName)]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ApiResponse> Register(
        [FromBody] RegisterRequest request)
    {
        await _authService.RegisterAsync(request);

        return new ApiResponse
        {
            IsSuccess = true,
            Message = "Registration successful. Please check your email to confirm your account."
        };
    }

    [HttpPost("login")]
    public async Task<ApiResponse<LoginResponse>> Login(
        [FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        return new ApiResponse<LoginResponse>(
            true,
            "Login successful.",
            result);
    }

    [HttpPost("refresh-token")]
    public async Task<ApiResponse<LoginResponse>> RefreshToken(
        [FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);

        return new ApiResponse<LoginResponse>(
            true,
            "Token refreshed successfully.",
            result);
    }

    [HttpPost("confirm-email")]
    public async Task<ApiResponse> ConfirmEmail(
        [FromBody] ConfirmEmailRequest request)
    {
        await _authService.ConfirmEmailAsync(request);

        return new ApiResponse
        {
            IsSuccess = true,
            Message = "Email confirmed successfully."
        };
    }

    [HttpPost("resend-confirmation")]
    public async Task<ApiResponse> ResendConfirmation(
        [FromBody] ResendConfirmationRequest request)
    {
        await _authService.ResendConfirmationAsync(request);

        return new ApiResponse
        {
            IsSuccess = true,
            Message = "If the account exists, a confirmation email was sent."
        };
    }

    [HttpPost("forgot-password")]
    public async Task<ApiResponse> ForgotPassword(
        [FromBody] ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request);

        return new ApiResponse
        {
            IsSuccess = true,
            Message = "If the account exists, a password reset email was sent."
        };
    }

    [HttpPost("reset-password")]
    public async Task<ApiResponse> ResetPassword(
        [FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request);

        return new ApiResponse
        {
            IsSuccess = true,
            Message = "Password reset successfully."
        };
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ApiResponse> ChangePassword(
        [FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            Response.StatusCode = 401;

            return new ApiResponse
            {
                IsSuccess = false,
                Message = "Unauthorized."
            };
        }

        await _authService.ChangePasswordAsync(
            userId,
            request);

        return new ApiResponse
        {
            IsSuccess = true,
            Message = "Password changed successfully."
        };
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ApiResponse> Logout(
        [FromBody] RefreshTokenRequest request)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            Response.StatusCode = 401;

            return new ApiResponse
            {
                IsSuccess = false,
                Message = "Unauthorized."
            };
        }

        await _authService.LogoutAsync(
            userId,
            request);

        return new ApiResponse
        {
            IsSuccess = true,
            Message = "Logged out successfully."
        };
    }

    [Authorize]
    [HttpGet("me")]
    public ApiResponse<object> Me()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        var email = User.FindFirstValue(
            ClaimTypes.Email);

        var username = User.FindFirstValue(
            ClaimTypes.Name);

        return new ApiResponse<object>(
            true,
            "User retrieved successfully.",
            new
            {
                Id = userId,
                Email = email,
                Username = username
            });
    }
}
