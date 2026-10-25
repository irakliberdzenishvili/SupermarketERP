using DotNetApiTemplate.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupermarketERP.Models.Data;

[Index(nameof(Code), IsUnique = true)]
public class Branch : ISoftDeletable
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [Required]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

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

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public BranchManager? BranchManager { get; set; }

    [InverseProperty(nameof(BranchWorker.Branch))]
    public ICollection<BranchWorker> Workers { get; set; }
        = new List<BranchWorker>();

    public Guid? WarehouseId { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    [InverseProperty(nameof(Warehouse.Branches))]
    public Warehouse? Warehouse { get; set; }
}