namespace MenuOptics.Models
{
    public class VendorInvoiceItemDTO
    {
        public int? IngredientId { get; set; }
        public int? PackSize { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Weight { get; set; }
        public decimal Price { get; set; }
    }
}
