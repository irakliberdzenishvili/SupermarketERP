
using DotNetApiTemplate.Interfaces;
using DotNetApiTemplate.Models.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupermarketERP.Models.Data;

[Index(nameof(UserId), IsUnique = true)]
[Index(nameof(WarehouseId), IsUnique = true)]
public class WarehouseManager : ISoftDeletable
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    public Guid WarehouseId { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}
