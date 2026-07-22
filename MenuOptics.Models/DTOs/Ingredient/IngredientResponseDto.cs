using System;

namespace MenuOptics.Models
{
    public class IngredientResponseDto
    {
        public int IngredientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsActive { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int? VendorId { get; set; }
        public int? UserId { get; set; }
        public int? AssignedRestaurantId { get; set; }
    }
}
