using System;
using System.Collections.Generic;

namespace MenuOptics.Models
{
    public class MenuAiAnalysisReportDto
    {
        public string? RestaurantName { get; set; }
        public string? Location { get; set; }
        public string? GeneratedDate { get; set; }
        public string? DateRange { get; set; }
        public int TotalMenuItemsAnalyzed { get; set; }
        public string? SourceModel { get; set; }
        public string? CompetitorMarketOverview { get; set; }

        public AiExecutiveSummaryDto ExecutiveSummary { get; set; } = new();
        public AiQuadrantBreakdownDto QuadrantBreakdown { get; set; } = new();
        public List<AiRemovalRecommendationDto> RemovalRecommendations { get; set; } = new();
        public List<AiPricingOptimizationDto> PricingOptimizations { get; set; } = new();
        public List<AiTitleImprovementDto> TitleImprovements { get; set; } = new();
        public List<AiActionStepDto> ActionPlan { get; set; } = new();
    }

    public class AiExecutiveSummaryDto
    {
        public int HealthScore { get; set; }
        public decimal EstimatedMonthlyProfitUplift { get; set; }
        public decimal AverageFoodCostPct { get; set; }
        public string? TopStrength { get; set; }
        public string? CriticalRisk { get; set; }
        public string? KeyTakeaway { get; set; }
    }

    public class AiQuadrantBreakdownDto
    {
        public int StarsCount { get; set; }
        public decimal StarsRevenueShare { get; set; }
        public int PlowhorsesCount { get; set; }
        public decimal PlowhorsesRevenueShare { get; set; }
        public int PuzzlesCount { get; set; }
        public decimal PuzzlesRevenueShare { get; set; }
        public int DogsCount { get; set; }
        public decimal DogsRevenueShare { get; set; }
    }

    public class AiRemovalRecommendationDto
    {
        public int MenuItemId { get; set; }
        public string? Name { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal Cost { get; set; }
        public int UnitsSold { get; set; }
        public decimal FoodCostPct { get; set; }
        public string? Quadrant { get; set; }
        public string? PrimaryReason { get; set; }
        public string? ActionRecommendation { get; set; }
        public decimal EstimatedMonthlyLaborOrWasteSavings { get; set; }
    }

    public class AiPricingOptimizationDto
    {
        public int MenuItemId { get; set; }
        public string? Name { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal RecommendedPrice { get; set; }
        public decimal CurrentMargin { get; set; }
        public decimal ProjectedMargin { get; set; }
        public decimal EstimatedMonthlyProfitIncrease { get; set; }
        public string? PricingPrinciple { get; set; }
        public string? Rationale { get; set; }
    }

    public class AiTitleImprovementDto
    {
        public int MenuItemId { get; set; }
        public string? CurrentTitle { get; set; }
        public string? SuggestedTitle { get; set; }
        public string? TrendVerbiageUsed { get; set; }
        public string? Reasoning { get; set; }
    }

    public class AiActionStepDto
    {
        public string? Timeframe { get; set; }
        public string? ActionTitle { get; set; }
        public string? Details { get; set; }
        public string? ExpectedImpact { get; set; }
    }
}
