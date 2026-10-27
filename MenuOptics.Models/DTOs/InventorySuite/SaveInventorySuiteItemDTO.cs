using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuOptics.Models.DTOs
{
    public class SaveInventorySuiteItemDTO
    {
        public int IngredientId { get; set; }
        public decimal ClosingCount { get; set; }
        public decimal ClosingValue { get; set; }
    }
}
