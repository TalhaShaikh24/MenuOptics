using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuOptics.Models.DTOs.Ingredient
{
    public class CreateIngredientDTO
    {
        public int RestaurantId { get; set; }
        public int PrivateVendorId { get; set; }
        public string? VendorProductId { get; set; }
        public required string Name { get; set; }
        public decimal Price { get; set; }
        public required string Unit { get; set; }  // e.g., kg, liters, units
        public decimal Size { get; set; }
        public int? PackSize { get; set; }
    }
}
