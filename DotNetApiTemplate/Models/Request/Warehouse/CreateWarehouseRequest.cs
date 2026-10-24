
using System.ComponentModel.DataAnnotations;

namespace SupermarketERP.Models.Request.Warehouse;

public class CreateWarehouseRequest
{
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Phone]
    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    [StringLength(254)]
    public string? Email { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    public string WarehouseManagerUserId { get; set; }
        = string.Empty;

    public List<Guid> BranchIds { get; set; } = new();
}
