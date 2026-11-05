using SupermarketERP.Enums;

namespace SupermarketERP.Models.Response.Payments
{
    public class PaymentResponse
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime PaidAt { get; set; }
    }
}
