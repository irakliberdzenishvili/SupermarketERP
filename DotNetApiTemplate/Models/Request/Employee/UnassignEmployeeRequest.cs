using System.ComponentModel.DataAnnotations;

namespace SupermarketERP.Models.Request.Employee
{
    public class UnassignEmployeeRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
    }
}
