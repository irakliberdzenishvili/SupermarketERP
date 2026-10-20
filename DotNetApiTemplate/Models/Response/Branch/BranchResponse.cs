
using DotNetApiTemplate.Models.Data;
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

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public bool IsManager { get; set; }

    public User User { get; set; }

    public string? BranchManagerId { get; set; }

    public BranchManagerResponse? BranchManager { get; set; }

    public ICollection<Employee> Employees { get; set; }
        = new List<Employee>();

}