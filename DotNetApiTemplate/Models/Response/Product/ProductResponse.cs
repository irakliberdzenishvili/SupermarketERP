using System.ComponentModel.DataAnnotations;

namespace SupermarketERP.Models.Response.Product
{
    public class ProductResponse
    {
        public Guid Id { get; set; }

        public string SKU { get; set; } = null!;

        public string? Barcode { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public string Category { get; set; } = null!;

        public decimal PurchasePrice { get; set; }

        public decimal SellingPrice { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdateAt { get; set; } = DateTime.UtcNow;
    }
}
