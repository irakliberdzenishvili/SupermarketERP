using DotNetApiTemplate.Models.Data;

namespace DotNetApiTemplate.Interfaces
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
    }
}
