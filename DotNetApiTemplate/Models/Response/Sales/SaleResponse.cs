using SupermarketERP.Enums;
using SupermarketERP.Models.Response.Payments;

namespace SupermarketERP.Models.Response.Sales
{
    public class SaleResponse
    {
        public Guid Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public Guid BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string CashierId { get; set; } = string.Empty;
        public string CashierName { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public SaleStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<SaleItemResponse> Items { get; set; } = new();
        public List<PaymentResponse> Payments { get; set; } = new();
    }
}
