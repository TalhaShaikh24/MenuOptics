namespace Restaurant.Models
{
    public class Ingredient
    {
        public int IngredientId { get; set; }
        public int UserId { get; set; }
        public int VendorId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; } // e.g., kg, liters, units
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public bool? IsActive { get; set; }
        public string? FullName { get; set; }
    }

    public class IngredientDTO
    {
        public int IngredientId { get; set; }
        public int UserId { get; set; }
        public int VendorId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public bool? IsActive { get; set; }
        public string? FullName { get; set; }
    }
}