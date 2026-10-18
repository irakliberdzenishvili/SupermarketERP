
using DotNetApiTemplate.DB;
using DotNetApiTemplate.Exceptions;
using DotNetApiTemplate.Helpers;
using DotNetApiTemplate.Interfaces;
using DotNetApiTemplate.Models;
using DotNetApiTemplate.Models.Data;
using DotNetApiTemplate.Models.Request.Auth;
using DotNetApiTemplate.Models.Response.Auth;
using DotNetApiTemplate.Models.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace DotNetApiTemplate.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly SignInManager<User> _signInManager;
    private readonly JwtHelper _jwtHelper;

    public AuthService(
        UserManager<User> userManager,
        ApplicationDbContext db,
        IEmailService emailService,
        IConfiguration configuration,
        SignInManager<User> signInManager,
        JwtHelper jwtHelper)
    {
        _userManager = userManager;
        _db = db;
        _emailService = emailService;
        _configuration = configuration;
        _signInManager = signInManager;
        _jwtHelper = jwtHelper;
    }

    public async Task RegisterAsync(RegisterRequest request)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        try
        {
            var existingUser =
                await _userManager.FindByEmailAsync(request.Email);

            if (existingUser != null)
                throw new UserFriendlyException(
                    ErrorMessages.EmailAlreadyExists);

            var user = new User
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            var result = await _userManager.CreateAsync(
                user,
                request.Password);

            if (!result.Succeeded)
                ThrowIdentityError(result, ErrorMessages.InvalidInput);

            await SendConfirmationEmailAsync(user);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(
            request.Email);

        if (user == null)
            throw new UserFriendlyException(
                ErrorMessages.InvalidCredentials);

        if (!user.EmailConfirmed)
            throw new UserFriendlyException(
                ErrorMessages.EmailNotConfirmed);

        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
            throw new UserFriendlyException(
                ErrorMessages.AccountLocked);

        if (result.IsNotAllowed)
            throw new UserFriendlyException(
                ErrorMessages.AccountDisabled);

        if (!result.Succeeded)
            throw new UserFriendlyException(
                ErrorMessages.InvalidCredentials);

        return await CreateAuthResponseAsync(user);
    }

    public async Task<LoginResponse> RefreshTokenAsync(
        RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new UserFriendlyException(
                ErrorMessages.InvalidRefreshToken);

        var tokenHash = HashToken(request.RefreshToken);

        var storedToken = await _db.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

        if (storedToken == null)
            throw new UserFriendlyException(
                ErrorMessages.InvalidRefreshToken);

        if (storedToken.RevokedAt != null)
            throw new UserFriendlyException(
                ErrorMessages.RefreshTokenRevoked);

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
            throw new UserFriendlyException(
                ErrorMessages.RefreshTokenExpired);

        if (!storedToken.IsActive)
            throw new UserFriendlyException(
                ErrorMessages.InvalidRefreshToken);

        storedToken.RevokedAt = DateTime.UtcNow;

        return await CreateAuthResponseAsync(
            storedToken.User);
    }

    public async Task ConfirmEmailAsync(
        ConfirmEmailRequest request)
    {
        var user = await _userManager.FindByEmailAsync(
            request.Email);

        if (user == null)
            throw new UserFriendlyException(
                ErrorMessages.InvalidConfirmationCode);

        if (user.EmailConfirmed)
            throw new UserFriendlyException(
                ErrorMessages.EmailAlreadyConfirmed);

        var result = await _userManager.ConfirmEmailAsync(
            user,
            request.Token);

        if (!result.Succeeded)
            throw new UserFriendlyException(
                ErrorMessages.InvalidConfirmationCode);
    }

    public async Task ResendConfirmationAsync(
        ResendConfirmationRequest request)
    {
        var user = await _userManager.FindByEmailAsync(
            request.Email);

        if (user == null || user.EmailConfirmed)
            return;

        await SendConfirmationEmailAsync(user);
    }

    public async Task ForgotPasswordAsync(
        ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(
            request.Email);

        if (user == null)
            return;

        var token =
            await _userManager.GeneratePasswordResetTokenAsync(user);

        var baseUrl = _configuration["AppSettings:BaseUrl"]
            ?? throw new InvalidOperationException(
                "AppSettings:BaseUrl is not configured.");

        var resetUrl =
            $"{baseUrl}/reset-password" +
            $"?email={Uri.EscapeDataString(user.Email!)}" +
            $"&token={Uri.EscapeDataString(token)}";

        var html = $"""
            <h2>Password Reset</h2>

            <p>Hello {WebUtility.HtmlEncode(user.FirstName)},</p>

            <p>
                We received a request to reset your password.
            </p>

            <p>
                <a href="{WebUtility.HtmlEncode(resetUrl)}">
                    Reset Password
                </a>
            </p>

            <p>
                If you did not request this, you can safely ignore this email.
            </p>
            """;

        await _emailService.SendEmailAsync(
            user.Email!,
            "Reset your password",
            html);
    }

    public async Task ResetPasswordAsync(
        ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(
            request.Email);

        if (user == null)
            throw new UserFriendlyException(
                ErrorMessages.InvalidResetToken);

        var result = await _userManager.ResetPasswordAsync(
            user,
            request.Token,
            request.NewPassword);

        if (!result.Succeeded)
            ThrowIdentityError(
                result,
                ErrorMessages.InvalidResetToken);
    }

    public async Task ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            throw new UserFriendlyException(
                ErrorMessages.Unauthorized);

        if (request.CurrentPassword == request.NewPassword)
            throw new UserFriendlyException(
                ErrorMessages.NewPasswordMustBeDifferent);

        var result = await _userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);

        if (!result.Succeeded)
            ThrowIdentityError(
                result,
                ErrorMessages.OperationFailed);
    }

    public async Task LogoutAsync(
        string userId,
        RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new UserFriendlyException(
                ErrorMessages.InvalidRefreshToken);

        var tokenHash = HashToken(request.RefreshToken);

        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(x =>
                x.TokenHash == tokenHash &&
                x.UserId == userId);

        if (token == null)
            return;

        token.RevokedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }



    private async Task<LoginResponse> CreateAuthResponseAsync(
        User user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var role = roles.FirstOrDefault() ?? string.Empty;

        var accessToken = _jwtHelper.GenerateToken(
            user.Id,
            user.UserName ?? string.Empty,
            role);

        var refreshToken = _jwtHelper.GenerateRefreshToken();

        var accessExpiresAt = DateTime.UtcNow.AddMinutes(60);
        var refreshExpiresAt = DateTime.UtcNow.AddDays(7);

        var refreshTokenEntity = new RefreshToken
        {
            TokenHash = HashToken(refreshToken),
            ExpiresAt = refreshExpiresAt,
            UserId = user.Id
        };

        await _db.RefreshTokens.AddAsync(refreshTokenEntity);

        await _db.SaveChangesAsync();

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = accessExpiresAt,
            RefreshTokenExpiresAt = refreshExpiresAt
        };
    }



    private async Task SendConfirmationEmailAsync(
        User user)
    {
        var token =
            await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var baseUrl = _configuration["AppSettings:BaseUrl"]
            ?? throw new InvalidOperationException(
                "AppSettings:BaseUrl is not configured.");

        var confirmationUrl =
            $"{baseUrl}/confirm-email" +
            $"?email={Uri.EscapeDataString(user.Email!)}" +
            $"&token={Uri.EscapeDataString(token)}";

        var html = $"""
            <h2>Confirm your email</h2>

            <p>Hello {WebUtility.HtmlEncode(user.FirstName)},</p>

            <p>
                Please confirm your email address by clicking the button below.
            </p>

            <p>
                <a href="{WebUtility.HtmlEncode(confirmationUrl)}">
                    Confirm Email
                </a>
            </p>
            """;

        await _emailService.SendEmailAsync(
            user.Email!,
            "Confirm your email",
            html);
    }

    private static void ThrowIdentityError(
        IdentityResult result,
        ErrorMessages fallback)
    {
        var codes = result.Errors
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (codes.Contains("DuplicateEmail"))
            throw new UserFriendlyException(
                ErrorMessages.EmailAlreadyExists);

        if (codes.Contains("DuplicateUserName"))
            throw new UserFriendlyException(
                ErrorMessages.UserAlreadyExists);

        if (codes.Contains("InvalidEmail"))
            throw new UserFriendlyException(
                ErrorMessages.InvalidEmail);

        if (codes.Contains("PasswordMismatch"))
            throw new UserFriendlyException(
                ErrorMessages.IncorrectPassword);

        if (codes.Any(x => x.StartsWith(
                "Password",
                StringComparison.OrdinalIgnoreCase)))
            throw new UserFriendlyException(
                ErrorMessages.PasswordTooWeak);

        if (codes.Contains("InvalidToken"))
            throw new UserFriendlyException(fallback);

        throw new UserFriendlyException(fallback);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(bytes);
    }
}
