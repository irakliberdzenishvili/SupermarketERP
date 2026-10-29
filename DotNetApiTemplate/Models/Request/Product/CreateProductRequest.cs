using System.ComponentModel.DataAnnotations;

namespace SupermarketERP.Models.Request.Product
{
    public class CreateProductRequest
    {
        public string? Barcode { get; set; }

        [MaxLength(150)]
        [Required]
        public string Name { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        [Required]
        public string Category { get; set; } = null!;

        public decimal PurchasePrice { get; set; }

        public decimal SellingPrice { get; set; }

    }
}
