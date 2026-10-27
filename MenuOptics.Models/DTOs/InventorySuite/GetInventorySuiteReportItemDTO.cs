namespace MenuOptics.Models.DTOs
{
    public class GetInventorySuiteReportItemDTO
    {
        public int IngredientId { get; set; }
        public required string IngredientName { get; set; }
        public required string Unit { get; set; }
        public decimal Size { get; set; }
        public decimal Price { get; set; }
        public int? PackSize { get; set; }
        public required string VendorName { get; set; }
        public decimal OpeningCount { get; set; }
        public decimal OpeningValue { get; set; }
        public decimal PurchasesQuantity { get; set; }
        public decimal PurchasesValue { get; set; }
        public decimal ClosingCount { get; set; }
        public decimal ClosingValue { get; set; }
        public decimal Cogs { get; set; }
    }
}
