namespace MenuOptics.Models
{
    public class InvoiceItemDto
    {
        public int IngredientId { get; set; }
        public decimal? Weight { get; set; }
        public decimal? PackSize { get; set; }
        public decimal? Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
