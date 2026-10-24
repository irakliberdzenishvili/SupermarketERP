using System.ComponentModel.DataAnnotations;

namespace SupermarketERP.Models.Request;

public class AssignBranchWorkerRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;
}
