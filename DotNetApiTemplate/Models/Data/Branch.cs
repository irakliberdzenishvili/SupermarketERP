
using DotNetApiTemplate.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace SupermarketERP.Models.Data.Branch;

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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    public string? BranchManagerId { get; set; }

    public Employee? BranchManager { get; set; }

    public ICollection<Employee> Employees { get; set; }
        = new List<Employee>();
}
