using System.Collections.Generic;

namespace MenuOptics.Models
{
    public class AnalyticsDashboardDto
    {
        // Result Set 1: Monthly Revenue
        public decimal? CurrentRevenue { get; set; }
        public decimal? PreviousRevenue { get; set; }
        public decimal? PercentageChangeRevenue { get; set; }
        // Result Set 2: Monthly Sales Count
        public int? CurrentSalesCount { get; set; }
        public int? PreviousSalesCount { get; set; }
        public decimal? PercentageChangeSales { get; set; }

        // Result Set 3a: Average Order Value
        public decimal? CurrentAvgOrderValue { get; set; }
        public decimal? PreviousAvgOrderValue { get; set; }
        public decimal? PercentageChangeAvgOrder { get; set; }

        // Result Set 3b: Total Items Sold
        public int? CurrentItemsSold { get; set; }
        public int? PreviousItemsSold { get; set; }
        public decimal? PercentageChangeItems { get; set; }

        // Result Set 5: Revenue Trend
        public List<RevenueTrendDto>? RevenueTrends { get; set; }

        // Result Set 6: Pie Chart (Revenue Distribution)
        public List<PieChartDataDto>? RevenueDistribution { get; set; }

        // Result Set 7: Menu Performance
        public List<MenuPerformanceDto>? MenuPerformances { get; set; }
    }

    public class RevenueTrendDto
    {
        public int MonthNumber { get; set; }
        public decimal MonthlyRevenue { get; set; }
    }

    public class PieChartDataDto
    {
        public string? CategoryName { get; set; }
        public decimal CategoryRevenue { get; set; }
        public decimal PercentageShare { get; set; }
    }

    public class MenuPerformanceDto
    {
        public string? MenuName { get; set; }
        public decimal Revenue { get; set; }
        public int SalesCount { get; set; }
        public decimal Percentage { get; set; }
        public decimal CostPercentage { get; set; }
        public decimal ContributionMargin { get; set; }
        public decimal? Rating { get; set; }
    }
}
