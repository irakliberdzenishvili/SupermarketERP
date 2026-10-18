using System.ComponentModel.DataAnnotations;

namespace DotNetApiTemplate.Models.Request.Auth;

public class ResendConfirmationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}