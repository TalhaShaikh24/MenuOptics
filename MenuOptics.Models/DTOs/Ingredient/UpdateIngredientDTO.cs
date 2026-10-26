namespace MenuOptics.Models.DTOs    
{
    public class UpdateIngredientDTO
    {
        public int IngredientId { get; set; }
        public string? VendorProductId { get; set; }
        public required string Name { get; set; }
        public decimal Price { get; set; }
        public required string Unit { get; set; }
        public decimal Size { get; set; }
        public int? PackSize { get; set; }
    }
}
