using System;

namespace MenuOptics.Models
{
    public class ClosingCountUpdateDto
    {
        public int RestaurantId { get; set; }
        public int IngredientId { get; set; }
        public decimal ClosingCount { get; set; }
        public DateTime ClosingDate { get; set; }
    }
}
