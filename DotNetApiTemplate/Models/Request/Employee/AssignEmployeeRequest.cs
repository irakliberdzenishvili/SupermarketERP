using System.ComponentModel.DataAnnotations;

namespace SupermarketERP.Models.Request.AssignEmployeeRequest
{
    public class AssignEmployeeRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
    }
}
