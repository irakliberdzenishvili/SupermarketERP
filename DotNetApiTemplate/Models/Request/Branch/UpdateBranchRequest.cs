
using System.ComponentModel.DataAnnotations;

namespace SupermarketERP.Models.Request.Branch;

public class UpdateBranchRequest
{
    [StringLength(150)]
    public string? Name { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [Phone]
    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    [StringLength(254)]
    public string? Email { get; set; }
}
