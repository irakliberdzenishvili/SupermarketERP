using SupermarketERP.Models.Data;
using System.ComponentModel.DataAnnotations;

namespace SupermarketERP.Models.Response.Warehouse
{
    public class WarehouseResponse
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();
       
        public string Code { get; set; } = string.Empty;

       
        public string Name { get; set; } = string.Empty;

        
        public string Address { get; set; } = string.Empty;

       
        public string City { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        
        public string? Email { get; set; }

        
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public Guid? WarehouseManagerId { get; set; }

        public WarehouseWorkerResponse? WarehouseManager { get; set; }

        public bool IsManager { get; set; } = false;

        public bool IsAdmin { get; set; } = false;
    }
}
