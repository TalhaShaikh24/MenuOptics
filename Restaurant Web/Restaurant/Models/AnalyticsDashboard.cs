namespace Restaurant.Models
{
    public class AnalyticsDashboard
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
        public List<RevenueTrend>? RevenueTrends { get; set; }

        // Result Set 6: Pie Chart (Revenue Distribution)
        public List<PieChartData>? RevenueDistribution { get; set; }

        // Result Set 7: Menu Performance
        public List<MenuPerformance>? MenuPerformances { get; set; }
    }

    public class RevenueTrend
    {
        public int MonthNumber { get; set; }
        public decimal MonthlyRevenue { get; set; }
    }

    public class PieChartData
    {
        public string? CategoryName { get; set; }
        public decimal CategoryRevenue { get; set; }
        public decimal PercentageShare { get; set; }
    }

    public class MenuPerformance
    {
        public string? MenuName { get; set; }
        public decimal Revenue { get; set; }
        public int SalesCount { get; set; }
        public decimal Percentage { get; set; }
        public decimal? Rating { get; set; } // Added based on your HTML
    }
}