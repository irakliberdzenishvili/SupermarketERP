using DotNetApiTemplate.Models.Request.Auth;
using DotNetApiTemplate.Models.Response.Auth;

namespace DotNetApiTemplate.Interfaces;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequest request);

    Task<LoginResponse> LoginAsync(LoginRequest request);

    Task<LoginResponse> RefreshTokenAsync(
        RefreshTokenRequest request);

    Task ConfirmEmailAsync(
        ConfirmEmailRequest request);

    Task ResendConfirmationAsync(
        ResendConfirmationRequest request);

    Task ForgotPasswordAsync(
        ForgotPasswordRequest request);

    Task ResetPasswordAsync(
        ResetPasswordRequest request);

    Task ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request);

    Task LogoutAsync(
        string userId,
        RefreshTokenRequest request);
}