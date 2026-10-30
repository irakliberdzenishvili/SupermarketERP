using DotNetApiTemplate.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace SupermarketERP.Models.Data
{
    [Index(nameof(SKU), IsUnique = true)]
    [Index(nameof(Barcode), IsUnique = true)]
    [Index(nameof(Name), IsUnique = true)]
    public class Product : ISoftDeletable
    {
        [Key]
        public Guid Id { get; set; } = Guid.CreateVersion7();

        [Required]
        public string SKU { get; set; } = GenerateSku();

        public string? Barcode { get; set; }

        [MaxLength(150)]
        [Required]
        public string Name { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        [Required]
        public string Category { get; set; } = null!;

        [Precision(18, 2)]
        public decimal PurchasePrice { get; set; }

        [Precision(18, 2)]
        public decimal SellingPrice { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }


        public static string GenerateSku()
        {
            return $"PRD-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        }

    }
}
