
using DotNetApiTemplate.Interfaces;
using DotNetApiTemplate.Models.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupermarketERP.Models.Data;

[Index(nameof(UserId), IsUnique = true)]
public class BranchWorker : ISoftDeletable
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    // IDENTITY USER

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    // PERSONAL INFORMATION

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    // ASSIGNED BRANCH

    public Guid? BranchId { get; set; }

    [ForeignKey(nameof(BranchId))]
    [InverseProperty(nameof(Branch.Workers))]
    public Branch? Branch { get; set; }


    [InverseProperty(nameof(Branch.BranchManager))]
    public ICollection<Branch> ManagedBranches { get; set; }
        = new List<Branch>();

    public DateTime HireDate { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
