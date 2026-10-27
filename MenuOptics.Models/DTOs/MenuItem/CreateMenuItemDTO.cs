using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace MenuOptics.Models.DTOs
{
    public class CreateMenuItemDTO
    {
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
