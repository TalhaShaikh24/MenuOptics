namespace MenuOptics.Models.DTOs
{
    public class CreateIngredientDTO
    {
        public int RestaurantId { get; set; }
        public int PrivateVendorId { get; set; }
        public string? VendorProductId { get; set; }
        public required string Name { get; set; }
        public decimal Price { get; set; }
        public required string Unit { get; set; }  // e.g., kg, liters, units
        public decimal Size { get; set; }
        public int? PackSize { get; set; }
    }
}
