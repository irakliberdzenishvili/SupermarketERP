

using DotNetApiTemplate.Models.Data;
using SupermarketERP.Models.Data.Branch;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupermarketERP.Models.Response.EmployeeResponse
{
    public class EmployeeResponse
    {
        public Guid Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public Guid? BranchId { get; set; }

        public DateTime HireDate { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}