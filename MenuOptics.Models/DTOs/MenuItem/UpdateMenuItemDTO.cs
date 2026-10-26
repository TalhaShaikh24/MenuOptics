using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace MenuOptics.Models.DTOs.MenuItem
{
    public class UpdateMenuItemDTO
    {
        public int MenuItemId { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public decimal Cost { get; set; }
        public decimal SellingPrice { get; set; }
        public string? FeaturedImage { get; set; }
        public IFormFile? FeaturedImageFile { get; set; }
        public List<SaveMenuItemIngredientDTO>? Ingredients { get; set; }
        public List<SaveMenuItemRecipeDTO>? Recipes { get; set; }
    }
}
