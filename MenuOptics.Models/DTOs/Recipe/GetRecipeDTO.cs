using System;
using System.Collections.Generic;

namespace MenuOptics.Models.DTOs
{
    public class GetRecipeDTO
    {
        public int RecipeId { get; set; }
        public int RestaurantId { get; set; }
        public required string Name { get; set; }
        public required string Unit { get; set; }
        public double Cost { get; set; }
        public double CurrentCost { get; set; }
        public decimal Size { get; set; }
        public string? Description { get; set; }
        public List<GetRecipeIngredientDTO>? Ingredients { get; set; } = new();
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
    }
}
