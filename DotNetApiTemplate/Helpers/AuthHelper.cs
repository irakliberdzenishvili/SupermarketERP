using System.Security.Claims;

namespace DotNetApiTemplate.Helpers;

public class AuthHelper(IHttpContextAccessor httpContextAccessor)
{
    public int UserId => GetUserId();
    public string UserName => GetUserName();
    public string UserRole => GetUserRole();

    private int GetUserId()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null) return -1;

        if (!int.TryParse(httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return -1;
        return userId;
    }

    private string GetUserName()
    {
        var userName = httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;
        return userName ?? string.Empty;
    }

    private string GetUserRole()
    {
        var userRole = httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
        return userRole ?? string.Empty;
    }
}