namespace MenuOptics.Models
{
    public class InvoiceItemDto
    {
        public int InventoryInvoiceId { get; set; }
        public int? IngredientId { get; set; }
        public string? VendorProductId { get; set; }
        public string? Name { get; set; }
        public string? Unit { get; set; }
        public decimal? Size { get; set; }
        public int? PackSize { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Weight { get; set; }
        public decimal Price { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
