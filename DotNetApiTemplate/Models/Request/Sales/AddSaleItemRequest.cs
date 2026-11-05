namespace SupermarketERP.Models.Request.Sales
{
    public class AddSaleItemRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
