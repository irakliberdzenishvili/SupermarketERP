using System.ComponentModel.DataAnnotations;

namespace SupermarketERP.Models.Request.Inventory
{
    public class AdjustInventoryRequest
    {
        [Required]
        public int QuantityChange { get; set; }

        [Required]
        public string Reason { get; set; }
    }
}
