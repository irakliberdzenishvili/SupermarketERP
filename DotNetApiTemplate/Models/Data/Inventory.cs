using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace SupermarketERP.Models.Data
{
    [Index(nameof(ProductId), nameof(WarehouseId), IsUnique = true)]
    [Index(nameof(ProductId), nameof(BranchId), IsUnique = true)]
    public class Inventory
    {
        [Key]
        public Guid Id { get; set; } = Guid.CreateVersion7();

        [Required]
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public Guid? WarehouseId { get; set; }

        public Guid? BranchId { get; set; }


        [Required]
        public int Quantity { get; set; }

        [Required]
        public int MinimumQuantity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }

        public ICollection<InventoryAdjustment> InventoryAdjustments { get; set; }


    }
}
