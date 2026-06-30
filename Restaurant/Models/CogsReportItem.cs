namespace MenuOptics.Models
{
    public class CogsReportRequest
    {
        public int RestaurantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class CogsReportItem
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; }
        public string Unit { get; set; }
        public string? UnitOfMeasure { get; set; }
        public string VendorName { get; set; }
        public decimal OpeningCount { get; set; }
        public decimal OpeningValue { get; set; }
        public decimal ClosingCount { get; set; }
        public decimal ClosingValue { get; set; }
        public decimal PurchasesQuantity { get; set; }
        public decimal PurchasesValue { get; set; }
        public decimal Cogs { get; set; }
        
    }
    public class TotalCogsReportItem
    {
        public decimal TotalCogs { get; set; }
        public decimal TotalFoodRevenue { get; set; }
        public decimal TheoreticalFoodCostPercentage { get; set; }
        public decimal ActualFoodCostPercentage { get; set; }
        public decimal VariancePercentage { get; set; }
        public decimal VarianceValue { get; set; }

    }
}