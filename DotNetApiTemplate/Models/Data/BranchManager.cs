using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DotNetApiTemplate.Interfaces;
using DotNetApiTemplate.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace SupermarketERP.Models.Data;

[Index(nameof(BranchId), IsUnique = true)]
[Index(nameof(UserId), IsUnique = true)]
public class BranchManager : ISoftDeletable
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    public Guid BranchId { get; set; }

    [ForeignKey(nameof(BranchId))]
    public Branch Branch { get; set; } = null!;

    public DateTime AssignedAt { get; set; }
        = DateTime.UtcNow;

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }
}