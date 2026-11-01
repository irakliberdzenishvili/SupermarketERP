using SupermarketERP.Enums;
using SupermarketERP.Models.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupermarketERP.Models.Request.Stock
{
    public class CreateStockTransferRequest
    {
        [Required]
        public Guid ProductId { get; set; }

        [Required]
        public Guid FromWarehouseId { get; set; }

        [Required]
        public Guid ToBranchId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
