using SupermarketERP.Enums;
using SupermarketERP.Models.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupermarketERP.Models.Response.Stock
{
    public class StockTransferResponse
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        public Guid FromWarehouseId { get; set; }

        public Guid ToBranchId { get; set; }

        public int Quantity { get; set; }

        public StockTransferStatus Status { get; set; }

        public string RequestedByUserId { get; set; }

        public string? ApprovedByUserId { get; set; }

        public string? DispatchedByUserId { get; set; }

        public string? ReceivedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public DateTime? DispatchedAt { get; set; }

        public DateTime? ReceivedAt { get; set; }
    }
}
