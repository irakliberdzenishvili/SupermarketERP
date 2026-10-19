using System.Security.Claims;

namespace DotNetApiTemplate.Helpers;

public class AuthHelper(IHttpContextAccessor httpContextAccessor)
{
    public string? UserId => GetUserId();
    public string UserName => GetUserName();
    public string UserRole => GetUserRole();

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?
            .User.Identity?.IsAuthenticated == true;

    private string? GetUserId()
    {
        var user = httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return user.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private string GetUserName()
    {
        return httpContextAccessor.HttpContext?
            .User.FindFirstValue(ClaimTypes.Name)
            ?? string.Empty;
    }

    private string GetUserRole()
    {
        return httpContextAccessor.HttpContext?
            .User.FindFirstValue(ClaimTypes.Role)
            ?? string.Empty;
    }
}