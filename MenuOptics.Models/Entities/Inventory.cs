using System;

namespace MenuOptics.Models
{
    public class Inventory
    {
        public int InventoryId { get; set; }
        public int RestaurantId { get; set; }
        public int? VendorId { get; set; }
        public int IngredientId { get; set; }
        public string? IngredientName { get; set; }
        public decimal Stock { get; set; }
        public decimal? Price { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
    }
}
