using SupermarketERP.Models.Data;
using System.ComponentModel.DataAnnotations;

namespace SupermarketERP.Models.Response.Warehouse
{
    public class WarehouseResponse
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

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public Guid? WarehouseManagerId { get; set; }

        public EmployeeResponse? WarehouseManager { get; set; }

        public bool IsManager { get; set; } = false;
    }
}
