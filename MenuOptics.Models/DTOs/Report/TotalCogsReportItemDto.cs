namespace MenuOptics.Models
{
    public class TotalCogsReportItemDto
    {
        public decimal TotalCogs { get; set; }
        public decimal TotalFoodRevenue { get; set; }
        public decimal TheoreticalFoodCostPercentage { get; set; }
        public decimal ActualFoodCostPercentage { get; set; }
        public decimal VariancePercentage { get; set; }
        public decimal VarianceValue { get; set; }
    }
}
