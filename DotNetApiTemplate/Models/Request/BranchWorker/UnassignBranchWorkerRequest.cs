using System.ComponentModel.DataAnnotations;

namespace SupermarketERP.Models.Request
{ 
    public class UnassignBranchWorkerRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
    }
}
