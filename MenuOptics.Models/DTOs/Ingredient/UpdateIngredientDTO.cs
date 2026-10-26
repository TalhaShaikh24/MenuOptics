using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuOptics.Models.DTOs.Ingredient
{
    public class UpdateIngredientDTO
    {
        public int IngredientId { get; set; }
        public string? VendorProductId { get; set; }
        public required string Name { get; set; }
        public decimal Price { get; set; }
        public required string Unit { get; set; }
        public decimal Size { get; set; }
        public int? PackSize { get; set; }
    }
}
