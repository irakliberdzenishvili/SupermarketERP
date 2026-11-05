using Microsoft.EntityFrameworkCore;

namespace SupermarketERP.Models.Data
{
    [Index(nameof(SaleId), nameof(ProductId), IsUnique = true)]
    public class SaleItem
    {
        public Guid Id { get; set; }
        public Guid SaleId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }

        public Sale Sale { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
