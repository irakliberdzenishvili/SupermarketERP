

using SupermarketERP.Models.Response;
using SupermarketERP.Models.Response.BranchManagerResponse;

public class BranchResponse
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }

    public bool IsActive { get; set; }

    public bool IsManager { get; set; } = false;

    public bool IsAdmin { get; set; } = false;

    public string? BranchManagerId { get; set; }

    public List<EmployeeResponse> Employees { get; set; } = [];
    public BranchManagerResponse? Manager { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}