using System;

namespace MenuOptics.Models
{
    public class InventorySuiteUpdateDto
    {
        public int RestaurantId { get; set; }
        public int IngredientId { get; set; }
        public decimal ClosingCount { get; set; }
        public decimal ClosingValue { get; set; }
        public DateTime ClosingDate { get; set; }
    }
}
