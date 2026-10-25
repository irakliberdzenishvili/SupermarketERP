using SupermarketERP.Constants;
using System.ComponentModel.DataAnnotations;

namespace SupermarketERP.Models.Request.Employee;

public class CreateEmployeeRequest
{
    [Required]
    public string UserId { get; set; }

    [Required]
    public string Role { get; set; } = Roles.User;

    public Guid? BranchId { get; set; }

    public Guid? WarehouseId { get; set; }
}