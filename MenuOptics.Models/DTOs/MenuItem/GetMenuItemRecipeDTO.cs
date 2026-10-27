using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuOptics.Models.DTOs
{
    public class GetMenuItemRecipeDTO
    {
        public int RecipeId { get; set; }
        public required string RecipeName { get; set; }
        public decimal Quantity { get; set; }
        public required string Unit { get; set; }
    }
}
