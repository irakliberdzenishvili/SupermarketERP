using SupermarketERP.Enums;

namespace SupermarketERP.Models.Request.Sales
{
    public class GetSalesRequest
    {
        public Guid? BranchId { get; set; }
        public string? CashierId { get; set; }
        public SaleStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Search { get; set; }
    }

}
