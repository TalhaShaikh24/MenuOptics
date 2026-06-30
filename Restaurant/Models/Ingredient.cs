using System.ComponentModel.DataAnnotations;

namespace MenuOptics.Models
{
    public class Ingredient
    {
        public int IngredientId { get; set; }
        public int UserId { get; set; }
        public int VendorId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; } // e.g., kg, liters, units
        public string? UnitOfMeasure { get; set; } // e.g., kg, liters, units
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
        public string? FullName { get; set; }
        public string? Stock { get; set; }
    }


    public class RestaurantPriceEntry
    {
        public int RestaurantId { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
    }

    public class VendorIngredientRequest
    {
        public string Name { get; set; }
        public string Unit { get; set; }

        public decimal? Weight { get; set; }
        public decimal? PackSize { get; set; }
        public decimal BasePrice { get; set; }
        public string? VendorProductId { get; set; } //Optional

        public List<RestaurantPriceEntry>? RestaurantPrices { get; set; }
    }

    public class IngredientResponse
    {
        public int IngredientId { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsActive { get; set; }
        public string FullName { get; set; }
        public int? VendorId { get; set; }
        public int? UserId { get; set; }
        public int? AssignedRestaurantId { get; set; }
    }

    public class UpdateIngredientPriceRequest
    {
        public int IngredientId { get; set; }

        public decimal? PackSize { get; set; }
        public decimal? Weight { get; set; }
        public decimal BasePrice { get; set; }

        public List<RestaurantPriceEntry>? RestaurantPrices { get; set; }
    }


    public class LinkVendorRequest
    {
        public int? Id { get; set; }
        public int? VendorId { get; set; }

        public int? VendorIngredientId { get; set; }

        public int? RestaurantId { get; set; }

        public decimal Price { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public int? CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public int? ModifiedBy { get; set; }

        public bool IsActive { get; set; } = true;

        public Ingredient? VendorIngredient { get; set; }

        public User? Restaurant { get; set; }
    }


    // DTO for patching ingredient name, unit, and/or status
    public class UpdateIngredientDetailsRequest
    {
        public int IngredientId { get; set; }
        public string? Name { get; set; }
        public string? Unit { get; set; }
        public bool? IsActive { get; set; }
    }

}