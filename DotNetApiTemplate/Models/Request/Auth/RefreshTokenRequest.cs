using System.ComponentModel.DataAnnotations;

namespace DotNetApiTemplate.Models.Request.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}