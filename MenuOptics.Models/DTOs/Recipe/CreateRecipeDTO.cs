using System;
using System.Collections.Generic;

namespace MenuOptics.Models.DTOs.Recipe
{
    public class CreateRecipeDTO
    {
        public required string Name { get; set; }
        public required string Unit { get; set; }
        public double Cost { get; set; }
        public decimal Size { get; set; }
        public string? Description { get; set; }
        public List<SaveRecipeIngredientDTO>? Ingredients { get; set; } = new();
    }
}
