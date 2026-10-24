
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using DotNetApiTemplate.Models.Data;
using DotNetApiTemplate.Interfaces;

namespace SupermarketERP.Models.Data;

[Index(nameof(UserId), IsUnique = true)]
[Index(nameof(WarehouseId))]
public class WarehouseWorker : ISoftDeletable
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public Guid WarehouseId { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    public DateTime HireDate { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
