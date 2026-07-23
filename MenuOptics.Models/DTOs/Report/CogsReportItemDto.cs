namespace MenuOptics.Models
{
    public class CogsReportItemDto
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Size { get; set; }
        public int? PackSize { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public decimal OpeningCount { get; set; }
        public decimal OpeningValue { get; set; }
        public decimal PurchasesQuantity { get; set; }
        public decimal PurchasesValue { get; set; }
        public decimal ClosingCount { get; set; }
        public decimal ClosingValue { get; set; }
        public decimal Cogs { get; set; }
    }
}
