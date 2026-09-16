using System;

namespace MenuOptics.Models
{
    public class Ingredient
    {
        public int IngredientId { get; set; }
        public int RestaurantId { get; set; }
        public int VendorId { get; set; }
        public int? PrivateVendorId { get; set; }
        public string? VendorProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? VendorName { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; } = string.Empty; // e.g., kg, liters, units
        public decimal Size { get; set; } 
        public int? PackSize { get; set; }
        public int ClassificationId { get; set; }
        public string? ClassificationDescriptor { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public int TotalCount { get; set; }
        public Ingredient? VendorIngredient { get; set; }
        public User? Restaurant { get; set; }
    }
}
