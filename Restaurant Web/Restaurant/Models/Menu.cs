using Microsoft.AspNetCore.Http;

namespace Restaurant.Models
{
    public class Menu
    {
        public int MenuItemId { get; set; }
        public int UserId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? CategoryName { get; set; }
        public int CategoryId { get; set; }
        public decimal Cost { get; set; }
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
    }

    public class MenuDTO
    {
        public int MenuItemId { get; set; }
        public int UserId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public decimal Cost { get; set; }
        public decimal SellingPrice { get; set; }
        public string? FeaturedImage { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
        public string FullName { get; set; }
        public List<MenuItemIngredientDTO>? Ingredients { get; set; }
    }

    public class MenuItemIngredient
    {
        public int MenuItemId { get; set; }
        public int IngredientId { get; set; }
        public decimal Quantity { get; set; }
    }

    public class MenuItemIngredientDTO
    {
        public int MenuItemId { get; set; }
        public int IngredientId { get; set; }
        public string IngredientName { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
    }
}