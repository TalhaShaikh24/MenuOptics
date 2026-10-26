using System.Collections.Generic;

namespace MenuOptics.Models
{
    public class UpdateVendorIngredientPriceDTO
    {
        public int IngredientId { get; set; }
        public decimal? PackSize { get; set; }
        public decimal? Weight { get; set; }
        public decimal BasePrice { get; set; }
        public List<RestaurantPriceEntryDTO>? RestaurantPrices { get; set; }
    }
}
