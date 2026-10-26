using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuOptics.Models.DTOs.MenuItem
{
    public class GetMenuItemIngredientDTO
    {
        public int MenuItemId { get; set; }
        public int IngredientId { get; set; }
        public decimal Quantity { get; set; }
        public required string Unit { get; set; }
        public required string UOM { get; set; }
        public string? Source { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal? OldPrice { get; set; }
    }
}
