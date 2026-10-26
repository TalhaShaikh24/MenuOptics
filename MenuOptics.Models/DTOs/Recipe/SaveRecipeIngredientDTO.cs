using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuOptics.Models.DTOs.Recipe
{
    public class SaveRecipeIngredientDTO
    {
        public int IngredientId { get; set; }
        public decimal Quantity { get; set; }
        public required string UOM { get; set; }
    }
}
