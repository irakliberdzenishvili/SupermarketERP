using SupermarketERP.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupermarketERP.Models.Data
{
    public class StockTransfer
    {
        [Key]
        public Guid Id { get; set; } = Guid.CreateVersion7();

        [Required]
        public Guid ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

        [Required]
        public Guid FromWarehouseId { get; set; }

        [ForeignKey(nameof(FromWarehouseId))]
        public Warehouse FromWarehouse { get; set; } = null!;

        [Required]
        public Guid ToBranchId { get; set; }

        [ForeignKey(nameof(ToBranchId))]
        public Branch ToBranch { get; set; } = null!;

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public StockTransferStatus Status { get; set; } = StockTransferStatus.Pending;

        [Required]
        public string RequestedByUserId { get; set; } = string.Empty;

        public string? ApprovedByUserId { get; set; }

        public string? DispatchedByUserId { get; set; }

        public string? ReceivedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }

        public DateTime? DispatchedAt { get; set; }

        public DateTime? ReceivedAt { get; set; }
    }
}