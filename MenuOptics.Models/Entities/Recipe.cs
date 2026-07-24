using System;
using System.Collections.Generic;

namespace MenuOptics.Models
{
    public class Recipe
    {
        public int RecipeId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public double Price { get; set; }
        public double CurrentPrice { get; set; }
        public string? Description { get; set; }
        public List<RecipeIngredient> Ingredients { get; set; } = new();
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
    }

    public class RecipeIngredient
    {
        public int IngredientId { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Uom { get; set; } = string.Empty;
        public string IngredientName { get; set; } = string.Empty;
    }
}
