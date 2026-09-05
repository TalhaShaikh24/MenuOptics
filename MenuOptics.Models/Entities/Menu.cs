using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace MenuOptics.Models
{
    public class Menu
    {
        public int MenuItemId { get; set; }
        public int RestaurantId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public decimal Cost { get; set; }
        public decimal CurrentCost { get; set; }
        public decimal SellingPrice { get; set; }
        public string? FeaturedImage { get; set; }
        public IFormFile? FeaturedImageFile { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
        public List<MenuItemIngredient>? Ingredients { get; set; }
        public List<MenuItemRecipe>? Recipes { get; set; }
    }

    public class MenuItemIngredient
    {
        public int MenuItemId { get; set; }
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Uom { get; set; } = string.Empty;

        public string? Source { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal? OldPrice { get; set; }
    }

    public class MenuItemRecipe
    {
        public int RecipeId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Uom { get; set; } = string.Empty;
    }
}
