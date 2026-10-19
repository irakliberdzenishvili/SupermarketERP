using System.ComponentModel.DataAnnotations;

namespace SupermarketERP.Models.Request.Branch;

public class AssignBranchManagerRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;
}