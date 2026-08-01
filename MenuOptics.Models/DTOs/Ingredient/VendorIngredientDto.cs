using System.Collections.Generic;

namespace MenuOptics.Models
{
    public class VendorIngredientDto
    {
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal? Weight { get; set; }
        public decimal Size { get; set; }
        public int? PackSize { get; set; }
        public decimal BasePrice { get; set; }
        public string? VendorProductId { get; set; } //Optional
        public int ClassificationId { get; set; }
        public string? ClassificationDescriptor { get; set; }
        public List<RestaurantPriceEntryDto>? RestaurantPrices { get; set; }
    }
}
