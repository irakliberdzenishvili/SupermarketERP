

using DotNetApiTemplate.Models.Data;
using SupermarketERP.Models.Data.Branch;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupermarketERP.Models.Response.EmployeeResponse
{
    public class EmployeeResponse
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();

        public string UserId { get; set; } = string.Empty;

        public User User { get; set; } = null!;

        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        public Guid? BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public Branch Branch { get; set; }

        public DateTime HireDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
