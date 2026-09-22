using System.Collections.Generic;

namespace MenuOptics.Models
{
    public class UpdateInventorySuiteReportRequestDto
    {
        public int InventorySuiteId { get; set; }
        public List<InventorySuiteItemUpdateDto> Items { get; set; } = new();
    }

    public class InventorySuiteItemUpdateDto
    {
        public int IngredientId { get; set; }
        public decimal ClosingCount { get; set; }
        public decimal ClosingValue { get; set; }
    }
}
