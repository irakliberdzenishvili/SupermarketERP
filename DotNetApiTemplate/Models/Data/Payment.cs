using DotNetApiTemplate.Interfaces;
using SupermarketERP.Enums;

namespace SupermarketERP.Models.Data
{
    public class Payment : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid SaleId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
        public PaymentStatus Status { get; set; } = PaymentStatus.Completed;
        public DateTime PaidAt { get; set; } = DateTime.UtcNow;
        public string? ReferenceNumber { get; set; }

        public Sale Sale { get; set; } = null!;

        public bool IsDeleted { get; set; }

    }
}
