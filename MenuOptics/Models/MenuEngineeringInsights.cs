namespace MenuOptics.Models
{
    public class MenuEngineeringInsights
    {
        public int UserId { get; set; }
        public int MenuItemId { get; set; }
        public bool isActive { get; set; }
        public string? Name { get; set; }
        public string? FeaturedImage { get; set; }
        public decimal? SellingPrice { get; set; }
        public decimal? Cost { get; set; }
        public int UnitsSold { get; set; }
        public decimal ContributionMargin { get; set; }
        public decimal ProfitabilityScore { get; set; }
        public decimal PopularityShare { get; set; }
        public string? MenuQuadrant { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }
}
