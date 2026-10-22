using SupermarketERP.Models.Data;
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

    [Required]
    public Guid WarehouseManagerId { get; set; }

    [Required]
    public List<Guid> BranchIds { get; set; } = [];

}