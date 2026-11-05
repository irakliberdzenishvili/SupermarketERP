using DotNetApiTemplate.Models.Data;
using Microsoft.EntityFrameworkCore;
using SupermarketERP.Enums;
using System.Net.ServerSentEvents;

namespace SupermarketERP.Models.Data
{
    [Index(nameof(SaleNumber), IsUnique = true)]
    public class Sale
    {
        public Guid Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public Guid BranchId { get; set; }
        public string CashierId { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public SaleStatus Status { get; set; } = SaleStatus.Draft;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public Branch Branch { get; set; } = null!;
        public User Cashier { get; set; } = null!;
        public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
