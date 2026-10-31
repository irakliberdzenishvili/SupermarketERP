using System.ComponentModel.DataAnnotations;

namespace SupermarketERP.Models.Response;

public class InventoryResponse
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Guid? WarehouseId { get; set; }

    public Guid? BranchId { get; set; }

    public int Quantity { get; set; }

    public int MinimumQuantity { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
