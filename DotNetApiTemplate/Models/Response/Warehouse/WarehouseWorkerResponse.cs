namespace SupermarketERP.Models.Response;

public class WarehouseWorkerResponse
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public DateTime HireDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}