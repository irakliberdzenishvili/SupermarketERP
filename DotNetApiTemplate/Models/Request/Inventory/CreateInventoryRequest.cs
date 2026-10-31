using SupermarketERP.Models.Data;
using System.ComponentModel.DataAnnotations;

namespace SupermarketERP.Models.Request;

public class CreateInventoryRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    public int MinimumQuantity { get; set; }
}
