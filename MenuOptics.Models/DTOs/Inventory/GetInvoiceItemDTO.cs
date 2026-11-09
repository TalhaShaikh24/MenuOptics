namespace MenuOptics.Models.DTOs
{
    public class GetInvoiceItemDTO
    {
        public int IngredientId { get; set; }
        public string? VendorProductId { get; set; }
        public required string IngredientName { get; set; }
        public required string Unit { get; set; }
        public decimal Size { get; set; }
        public int? PackSize { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Weight { get; set; }
        public decimal Price { get; set; } //Total price
    }
}
