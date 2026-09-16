namespace MenuOptics.Models
{
    public class InvoiceItemDto
    {
        public int InventoryInvoiceId { get; set; }
        public int IngredientId { get; set; }
        public decimal? Weight { get; set; }
        public int? PackSize { get; set; }
        public int? Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
