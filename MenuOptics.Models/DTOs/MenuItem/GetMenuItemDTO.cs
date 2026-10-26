using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace MenuOptics.Models.DTOs.MenuItem
{
    public class GetMenuItemDTO
    {
        public int MenuItemId { get; set; }
        public int RestaurantId { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public required string CategoryName { get; set; }
        public decimal Cost { get; set; }
        public decimal CurrentCost { get; set; }
        public decimal SellingPrice { get; set; }
        public string? FeaturedImage { get; set; }
        public IFormFile? FeaturedImageFile { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
        public List<GetMenuItemIngredientDTO>? Ingredients { get; set; }
        public List<GetMenuItemRecipeDTO>? Recipes { get; set; }
    }
}
