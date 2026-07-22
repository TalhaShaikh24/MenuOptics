using System.Collections.Generic;

namespace MenuOptics.Models
{
    public class UpdateIngredientPriceDto
    {
        public int IngredientId { get; set; }
        public decimal? PackSize { get; set; }
        public decimal? Weight { get; set; }
        public decimal BasePrice { get; set; }
        public List<RestaurantPriceEntryDto>? RestaurantPrices { get; set; }
    }
}
