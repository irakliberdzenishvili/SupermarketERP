using System.ComponentModel.DataAnnotations;

namespace SupermarketERP.Models.Request
{ 
    public class UnassignEmployeeRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
    }
}
