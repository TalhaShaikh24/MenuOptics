using System.Collections.Generic;

namespace MenuOptics.Models
{
    public class CreateVendorIngredientDTO
    {
        public required string Name { get; set; }
        public required string Unit { get; set; }
        public decimal? Weight { get; set; }
        public decimal Size { get; set; }
        public int? PackSize { get; set; }
        public decimal BasePrice { get; set; }
        public string? VendorProductId { get; set; } 
        public int ClassificationId { get; set; }
        public string? ClassificationDescriptor { get; set; }
        public List<RestaurantPriceEntryDTO>? RestaurantPrices { get; set; }
    }
}
