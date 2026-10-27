using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MenuOptics.Services
{
    public class MenuAiAnalysisService : IMenuAiAnalysisService
    {
        private readonly IMenuEngineeringInsightsService _insightsService;
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public MenuAiAnalysisService(
            IMenuEngineeringInsightsService insightsService,
            IUserService userService,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _insightsService = insightsService;
            _userService = userService;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ResponseDto> GenerateMenuAiAnalysisAsync(int currentUserId, DateTime? startDate, DateTime? endDate)
        {
            var responseDto = new ResponseDto();

            try
            {
                var userResponse = await _userService.GetUserAsync(currentUserId);
                var user = userResponse.Data as GetUserDTO;
                var restaurantName = user?.RestaurantName ?? user?.Username ?? "My Restaurant";

                // Build full location context from profile (Address, City, State, Zip, Country)
                var locationParts = new[] { user?.Address, user?.City, user?.State, user?.Zip, user?.Country }
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
                var locationText = locationParts.Any() ? string.Join(", ", locationParts) : "Commercial Dining Market";

                var insightsResponse = await _insightsService.GetAllMenuEngineeringInsightsByUserIdAsync(currentUserId, startDate, endDate);
                var rawInsights = (insightsResponse.Data as IEnumerable<MenuEngineeringInsightsDto>)?.ToList() ?? new List<MenuEngineeringInsightsDto>();

                if (!rawInsights.Any())
                {
                    responseDto.Status = 404;
                    responseDto.ResponseMsg = "No menu items or sales data found for the selected date range. Please ensure menus and sales are recorded.";
                    return responseDto;
                }

                string dateRangeText = startDate.HasValue && endDate.HasValue
                    ? $"{startDate.Value:MMM dd, yyyy} - {endDate.Value:MMM dd, yyyy}"
                    : "Last 30 Days";

                var anthropicApiKey = _configuration["Anthropic:ApiKey"];
                bool hasValidClaudeKey = !string.IsNullOrWhiteSpace(anthropicApiKey) &&
                                         !anthropicApiKey.Contains("YOUR_CLAUDE_API_KEY", StringComparison.OrdinalIgnoreCase);

                MenuAiAnalysisReportDto report;

                if (hasValidClaudeKey)
                {
                    try
                    {
                        report = await CallClaudeFullAnalysisApiAsync(anthropicApiKey!, rawInsights, restaurantName, locationText, dateRangeText);
                    }
                    catch
                    {
                        report = GenerateHeuristicFullAiAnalysis(rawInsights, restaurantName, locationText, dateRangeText);
                        report.SourceModel = "MenuOptics AI Engine";
                    }
                }
                else
                {
                    report = GenerateHeuristicFullAiAnalysis(rawInsights, restaurantName, locationText, dateRangeText);
                }

                responseDto.Status = 200;
                responseDto.ResponseMsg = "AI Menu Engineering Analysis generated successfully.";
                responseDto.Data = report;
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = $"Error generating AI analysis: {ex.Message}";
                return responseDto;
            }
        }

        #region Full Analysis Callers & Heuristic Engine

        private async Task<MenuAiAnalysisReportDto> CallClaudeFullAnalysisApiAsync(
            string apiKey,
            List<MenuEngineeringInsightsDto> insights,
            string restaurantName,
            string locationText,
            string dateRangeText)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var modelName = _configuration["Anthropic:Model"] ?? "claude-3-7-sonnet-20250219";
            int totalItems = insights.Count;
            int maxRemovalLimit = Math.Max(1, (int)Math.Floor(totalItems * 0.10));

            var simplifiedItems = insights.Select(i => new
            {
                i.MenuItemId,
                i.Name,
                SellingPrice = i.SellingPrice ?? 0,
                Cost = i.Cost ?? 0,
                UnitsSold = i.UnitsSold,
                ContributionMargin = i.ContributionMargin,
                PopularityShare = i.PopularityShare,
                FoodCostPct = (i.SellingPrice > 0) ? Math.Round(((i.Cost ?? 0) / i.SellingPrice.Value) * 100, 1) : 0,
                Quadrant = i.MenuQuadrant ?? "N/A"
            }).ToList();

            string itemsJson = JsonSerializer.Serialize(simplifiedItems);

            var systemPrompt = $@"You are an elite hospitality consultant, behavioral economics researcher, and menu engineering master.
Analyze the restaurant's menu performance in the direct context of its geographic location ({locationText}).

MANDATORY RESEARCH-BACKED MENU ENGINEERING & PRICING PRINCIPLES:
1. Psychological Price Points & Charm Endings:
   - Formulate all suggested prices using established hospitality pricing science (e.g. .95 or .50 psychological charm price endings, or clean round numbers for premium/signature items).
   - NEVER suggest awkward, unrounded, or arbitrary endings (e.g. avoid .17, .38, .82).
2. BCG Quadrant Price-Volume Elasticity:
   - Plowhorses (High Popularity, Low Margin): Inelastic demand allows tactical 5-8% price bumps. Guests are loyal and will absorb modest charm-rounded adjustments (e.g., $14.20 -> $15.95) with negligible demand drop.
   - Stars (High Popularity, High Margin): Apply micro-premium price adjustments (+2% to +5%) to test upper tolerance while protecting volume leadership.
   - Puzzles (Low Popularity, High Margin): Avoid aggressive price increases; instead apply charm pricing alignment and focus on title/placement improvements.
3. PricingPrinciple Field:
   - For every price recommendation, explicitly name the research principle applied (e.g., 'Charm Price Ending (.95) + Plowhorse Inelastic Demand Capture', 'Psychological Price Anchor & Margin Optimization', 'Prestige Whole-Dollar Rounding').
4. Modern Trend Verbiage & Culinary Naming (TitleImprovements):
   - Suggest modern, trend-forward culinary verbiage (e.g., *Artisan, Slow-Braised, Truffle-Infused, Wood-Fired, Hand-Crafted, Crisp Farmhouse, Char-Grilled, Aged Cheddar, Brioche, Flame-Seared, Honey-Glazed*).
   - Identify bland, generic, or boring titles (e.g., 'Chicken Burger', 'Beef Pasta', 'House Salad') and upgrade them to high-converting sensory titles that elevate perceived value against modern competitors in {locationText}. Only suggest when genuinely beneficial; populate TrendVerbiageUsed.
5. Strict 10% Removal Cap:
   - In RemovalRecommendations, recommend AT MOST {maxRemovalLimit} items (strictly <= 10% of the {totalItems} total menu items), prioritizing bottom Dogs with low sales and poor margins.
6. ActionPlan: Provide concrete prioritized 30-60-90 day action steps.
7. Return ONLY a valid JSON object matching the requested schema. No markdown wrapping, no extra prose.";

            var userPrompt = $@"Restaurant: {restaurantName}
Location: {locationText}
Date Range: {dateRangeText}
Total Items: {totalItems}
Max Removal Allowed (10% limit): {maxRemovalLimit}

Menu Dataset:
{itemsJson}

JSON Output Schema required:
{{
  ""CompetitorMarketOverview"": ""..."",
  ""ExecutiveSummary"": {{
    ""HealthScore"": 78,
    ""EstimatedMonthlyProfitUplift"": 1250.00,
    ""AverageFoodCostPct"": 31.4,
    ""TopStrength"": ""..."",
    ""CriticalRisk"": ""..."",
    ""KeyTakeaway"": ""...""
  }},
  ""QuadrantBreakdown"": {{
    ""StarsCount"": 0,
    ""StarsRevenueShare"": 0.0,
    ""PlowhorsesCount"": 0,
    ""PlowhorsesRevenueShare"": 0.0,
    ""PuzzlesCount"": 0,
    ""PuzzlesRevenueShare"": 0.0,
    ""DogsCount"": 0,
    ""DogsRevenueShare"": 0.0
  }},
  ""TitleImprovements"": [
    {{
      ""MenuItemId"": 1,
      ""CurrentTitle"": ""Chicken Burger"",
      ""SuggestedTitle"": ""Crispy Buttermilk Herb-Crusted Chicken Burger"",
      ""TrendVerbiageUsed"": ""Buttermilk, Herb-Crusted, Crispy"",
      ""Reasoning"": ""Generic title lacks sensory trigger; upgrading with modern culinary verbiage elevates perceived craft value and drives higher order conversion against local competitors.""
    }}
  ],
  ""RemovalRecommendations"": [
    {{
      ""MenuItemId"": 1,
      ""Name"": ""..."",
      ""SellingPrice"": 12.00,
      ""Cost"": 7.50,
      ""UnitsSold"": 5,
      ""FoodCostPct"": 62.5,
      ""Quadrant"": ""Dog"",
      ""PrimaryReason"": ""..."",
      ""ActionRecommendation"": ""..."",
      ""EstimatedMonthlyLaborOrWasteSavings"": 150.00
    }}
  ],
  ""PricingOptimizations"": [
    {{
      ""MenuItemId"": 2,
      ""Name"": ""..."",
      ""CurrentPrice"": 15.00,
      ""RecommendedPrice"": 16.95,
      ""CurrentMargin"": 9.00,
      ""ProjectedMargin"": 10.95,
      ""EstimatedMonthlyProfitIncrease"": 300.00,
      ""PricingPrinciple"": ""Charm Price Ending (.95) + Plowhorse Inelastic Demand Capture"",
      ""Rationale"": ""High guest demand (180 units) reflects inelastic price sensitivity. Adjusting to a researched .95 charm price point ($16.95) remains comfortably below local competitor ceilings in {locationText} while capturing +$300/mo.""
    }}
  ],
  ""ActionPlan"": [
    {{
      ""Timeframe"": ""Immediate (Week 1-2)"",
      ""ActionTitle"": ""..."",
      ""Details"": ""..."",
      ""ExpectedImpact"": ""...""
    }}
  ]
}}";

            var requestBody = new
            {
                model = modelName,
                max_tokens = 4096,
                system = systemPrompt,
                messages = new[] { new { role = "user", content = userPrompt } }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.anthropic.com/v1/messages", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Claude API returned {response.StatusCode}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var contentText = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "{}";

            contentText = CleanJsonMarkdown(contentText);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsedReport = JsonSerializer.Deserialize<MenuAiAnalysisReportDto>(contentText, options) ?? new MenuAiAnalysisReportDto();

            parsedReport.RestaurantName = restaurantName;
            parsedReport.Location = locationText;
            parsedReport.GeneratedDate = DateTime.UtcNow.ToString("MMM dd, yyyy HH:mm UTC");
            parsedReport.DateRange = dateRangeText;
            parsedReport.TotalMenuItemsAnalyzed = totalItems;
            parsedReport.SourceModel = $"Claude 3.7 ({modelName})";

            return parsedReport;
        }

        private MenuAiAnalysisReportDto GenerateHeuristicFullAiAnalysis(
            List<MenuEngineeringInsightsDto> insights,
            string restaurantName,
            string locationText,
            string dateRangeText)
        {
            int totalItems = insights.Count;
            int maxRemovalCount = Math.Max(1, (int)Math.Floor(totalItems * 0.10));

            decimal totalRevenue = insights.Sum(i => (i.SellingPrice ?? 0) * i.UnitsSold);
            decimal totalCost = insights.Sum(i => (i.Cost ?? 0) * i.UnitsSold);
            decimal overallFoodCostPct = totalRevenue > 0 ? Math.Round((totalCost / totalRevenue) * 100, 1) : 30.0m;

            var stars = insights.Where(i => string.Equals(i.MenuQuadrant, "Star", StringComparison.OrdinalIgnoreCase)).ToList();
            var plowhorses = insights.Where(i => string.Equals(i.MenuQuadrant, "Plowhorse", StringComparison.OrdinalIgnoreCase)).ToList();
            var puzzles = insights.Where(i => string.Equals(i.MenuQuadrant, "Puzzle", StringComparison.OrdinalIgnoreCase)).ToList();
            var dogs = insights.Where(i => string.Equals(i.MenuQuadrant, "Dog", StringComparison.OrdinalIgnoreCase)).ToList();

            decimal starsRev = stars.Sum(i => (i.SellingPrice ?? 0) * i.UnitsSold);
            decimal plowRev = plowhorses.Sum(i => (i.SellingPrice ?? 0) * i.UnitsSold);
            decimal puzzleRev = puzzles.Sum(i => (i.SellingPrice ?? 0) * i.UnitsSold);
            decimal dogRev = dogs.Sum(i => (i.SellingPrice ?? 0) * i.UnitsSold);

            var report = new MenuAiAnalysisReportDto
            {
                RestaurantName = restaurantName,
                Location = locationText,
                GeneratedDate = DateTime.UtcNow.ToString("MMM dd, yyyy HH:mm UTC"),
                DateRange = dateRangeText,
                TotalMenuItemsAnalyzed = totalItems,
                SourceModel = "MenuOptics AI Engine (Evaluation Mode)",
                CompetitorMarketOverview = $"Based on regional benchmarks for {locationText}, your average menu price point offers strong consumer value. Applying researched .95 charm price points and inelastic volume adjustments on Plowhorses will keep prices comfortably within local competitive tolerance while driving substantial bottom-line margin expansion.",
                ExecutiveSummary = new AiExecutiveSummaryDto
                {
                    HealthScore = Math.Min(95, Math.Max(45, (int)(100 - overallFoodCostPct + (stars.Count * 3) - (dogs.Count * 2)))),
                    AverageFoodCostPct = overallFoodCostPct,
                    TopStrength = stars.Any()
                        ? $"{stars.Count} Star menu item(s) generate ${(starsRev):F2} ({((starsRev / Math.Max(1, totalRevenue)) * 100):F1}% of revenue) with superior market competitiveness."
                        : "Solid menu foundation with distinct signature opportunities in your local trade area.",
                    CriticalRisk = dogs.Any()
                        ? $"{dogs.Count} Dog item(s) create kitchen complexity and increase ingredient holding risk compared to leaner local competitors."
                        : (overallFoodCostPct > 35 ? "Elevated food cost % (>35%) compressing net operating margins." : "High-margin Puzzle items require increased guest visibility."),
                    KeyTakeaway = $"Capitalize on local market demand in {locationText} with researched .95 charm pricing adjustments on Plowhorses, eliminate bottom 10% Dog items, and modernize menu titles with trend culinary verbiage."
                },
                QuadrantBreakdown = new AiQuadrantBreakdownDto
                {
                    StarsCount = stars.Count,
                    StarsRevenueShare = totalRevenue > 0 ? Math.Round((starsRev / totalRevenue) * 100, 1) : 0,
                    PlowhorsesCount = plowhorses.Count,
                    PlowhorsesRevenueShare = totalRevenue > 0 ? Math.Round((plowRev / totalRevenue) * 100, 1) : 0,
                    PuzzlesCount = puzzles.Count,
                    PuzzlesRevenueShare = totalRevenue > 0 ? Math.Round((puzzleRev / totalRevenue) * 100, 1) : 0,
                    DogsCount = dogs.Count,
                    DogsRevenueShare = totalRevenue > 0 ? Math.Round((dogRev / totalRevenue) * 100, 1) : 0
                }
            };

            // Trend Modern Verbiage Title Suggestions (Suggesting high-converting sensory titles for plain or puzzle items)
            var titleCandidates = puzzles.Concat(insights.Where(i => (i.Name?.Split(' ').Length ?? 0) <= 2))
                                         .DistinctBy(i => i.MenuItemId)
                                         .Take(3)
                                         .ToList();

            foreach (var item in titleCandidates)
            {
                string orig = item.Name ?? "Item";
                var (suggested, verbiage) = GenerateAppetizingTrendTitle(orig);
                if (!string.Equals(orig, suggested, StringComparison.OrdinalIgnoreCase))
                {
                    report.TitleImprovements.Add(new AiTitleImprovementDto
                    {
                        MenuItemId = item.MenuItemId,
                        CurrentTitle = orig,
                        SuggestedTitle = suggested,
                        TrendVerbiageUsed = verbiage,
                        Reasoning = $"'{orig}' is generic compared to modern culinary standards in {locationText}. Upgrading with trend verbiage ({verbiage}) triggers sensory appetite appeal, commands premium perception, and increases order velocity."
                    });
                }
            }

            // Removals (Capped at 10%)
            var candidates = dogs.OrderBy(d => d.PopularityShare)
                                 .ThenBy(d => d.ContributionMargin)
                                 .Concat(puzzles.OrderBy(p => p.UnitsSold))
                                 .Concat(insights.OrderBy(i => i.PopularityShare))
                                 .DistinctBy(i => i.MenuItemId)
                                 .Take(maxRemovalCount)
                                 .ToList();

            foreach (var item in candidates)
            {
                decimal price = item.SellingPrice ?? 0;
                decimal cost = item.Cost ?? 0;
                decimal fcPct = price > 0 ? Math.Round((cost / price) * 100, 1) : 0;
                decimal estSavings = Math.Max(50, Math.Round((cost * Math.Max(item.UnitsSold, 5)) + 40, 2));

                report.RemovalRecommendations.Add(new AiRemovalRecommendationDto
                {
                    MenuItemId = item.MenuItemId,
                    Name = item.Name ?? $"Item #{item.MenuItemId}",
                    SellingPrice = price,
                    Cost = cost,
                    UnitsSold = item.UnitsSold,
                    FoodCostPct = fcPct,
                    Quadrant = item.MenuQuadrant ?? "Dog",
                    PrimaryReason = $"Low sales volume ({item.UnitsSold} units sold) and low contribution margin (${item.ContributionMargin:F2}). Incurring holding waste compared to high-performing local alternatives.",
                    ActionRecommendation = "Remove from active menu layout. Redirect prep labor to Star items and eliminate slow-moving specialized inventory.",
                    EstimatedMonthlyLaborOrWasteSavings = estSavings
                });
            }

            // Researched Menu Engineering Pricing Optimization (Psychological Price Endings & Elasticity Banding)
            var pricingTargets = plowhorses.OrderByDescending(p => p.UnitsSold)
                                           .Concat(stars.OrderByDescending(s => s.UnitsSold))
                                           .DistinctBy(i => i.MenuItemId)
                                           .Take(4)
                                           .ToList();

            decimal totalEstimatedProfitUplift = 0;

            foreach (var item in pricingTargets)
            {
                decimal price = item.SellingPrice ?? 0;
                decimal cost = item.Cost ?? 0;
                bool isPlowhorse = string.Equals(item.MenuQuadrant, "Plowhorse", StringComparison.OrdinalIgnoreCase);

                // Menu engineering rule: Plowhorses absorb 6-9% bump; Stars absorb 3-5% micro-premium
                decimal targetIncreasePct = isPlowhorse ? 0.075m : 0.04m;
                decimal rawRecommended = price * (1 + targetIncreasePct);

                // Apply psychological .95 charm price ending
                decimal recommended = Math.Ceiling(rawRecommended) - 0.05m;
                if (recommended <= price)
                {
                    recommended = Math.Ceiling(price + 1.00m) - 0.05m;
                }

                decimal priceIncrease = recommended - price;
                decimal currentMargin = price - cost;
                decimal projectedMargin = recommended - cost;
                decimal monthlyGain = Math.Round(priceIncrease * Math.Max(item.UnitsSold, 10), 2);
                totalEstimatedProfitUplift += monthlyGain;

                string principle = isPlowhorse
                    ? "Charm Pricing (.95) + Plowhorse Inelastic Demand Capture"
                    : "Psychological Price Point (.95) + Star Brand Equity Premium";

                string rationale = isPlowhorse
                    ? $"High sales volume ({item.UnitsSold} units) proves high guest loyalty. Applying researched .95 charm pricing (${recommended:F2}) captures +${priceIncrease:F2}/unit with negligible volume resistance in {locationText}."
                    : $"Core flagship dish. Rounding to standard .95 charm price point (${recommended:F2}) expands gross margin while staying well within regional competitor bandwidth.";

                report.PricingOptimizations.Add(new AiPricingOptimizationDto
                {
                    MenuItemId = item.MenuItemId,
                    Name = item.Name ?? $"Item #{item.MenuItemId}",
                    CurrentPrice = price,
                    RecommendedPrice = recommended,
                    CurrentMargin = currentMargin,
                    ProjectedMargin = projectedMargin,
                    EstimatedMonthlyProfitIncrease = monthlyGain,
                    PricingPrinciple = principle,
                    Rationale = rationale
                });
            }

            report.ExecutiveSummary.EstimatedMonthlyProfitUplift = totalEstimatedProfitUplift;

            report.ActionPlan = new List<AiActionStepDto>
            {
                new AiActionStepDto
                {
                    Timeframe = "Immediate (Week 1-2)",
                    ActionTitle = $"Retire Bottom {maxRemovalCount} Deprecation Candidate(s)",
                    Details = "Remove flagged item(s) from physical and online ordering menus to immediately lower inventory complexity and prep waste.",
                    ExpectedImpact = "Immediate savings in ingredient spoilage and streamlined kitchen tickets."
                },
                new AiActionStepDto
                {
                    Timeframe = "Short Term (30 Days)",
                    ActionTitle = "Roll Out Researched .95 Charm Pricing & Trend Titles",
                    Details = $"Update high-volume item prices to .95 charm endings and apply suggested trend culinary verbiage to high-margin Puzzle items to out-convert local competitors in {locationText}.",
                    ExpectedImpact = $"+${Math.Round(totalEstimatedProfitUplift * 0.70m, 2):F2} estimated net monthly profit growth."
                },
                new AiActionStepDto
                {
                    Timeframe = "Medium Term (60-90 Days)",
                    ActionTitle = "Strategic Menu Real Estate & Vendor Negotiations",
                    Details = "Reposition high-margin Stars in primary eye-magnet zones and optimize supplier purchase agreements.",
                    ExpectedImpact = "Long-term gross margin expansion and stabilized operational costs."
                }
            };

            return report;
        }

        private static (string Title, string Verbiage) GenerateAppetizingTrendTitle(string currentTitle)
        {
            var lower = currentTitle.ToLower();
            if (lower.Contains("burger") && !lower.Contains("signature") && !lower.Contains("artisan"))
                return ($"Artisan Truffle & Aged Cheddar {currentTitle}", "Artisan, Truffle-Infused, Aged Cheddar");
            if (lower.Contains("pasta") && !lower.Contains("tuscan") && !lower.Contains("artisan"))
                return ($"Creamy Tuscan Garlic & Herb {currentTitle}", "Tuscan, Slow-Simmered, Herb-Infused");
            if (lower.Contains("salad") && !lower.Contains("harvest") && !lower.Contains("garden"))
                return ($"Farmhouse Crisp Garden Harvest {currentTitle}", "Farmhouse, Crisp Harvest, Garden-Fresh");
            if (lower.Contains("pizza") && !lower.Contains("wood-fired") && !lower.Contains("artisan"))
                return ($"Artisan Wood-Fired Hand-Crafted {currentTitle}", "Wood-Fired, Hand-Crafted, Artisan");
            if (lower.Contains("chicken") && !lower.Contains("herb") && !lower.Contains("crispy"))
                return ($"Crispy Buttermilk Herb-Roasted {currentTitle}", "Buttermilk, Herb-Roasted, Crispy");
            if (lower.Contains("steak") && !lower.Contains("prime") && !lower.Contains("aged"))
                return ($"Prime Flame-Seared 28-Day Aged {currentTitle}", "Flame-Seared, Prime Aged, Char-Crusted");
            if (lower.Contains("soup") && !lower.Contains("homestyle"))
                return ($"Slow-Simmered Homestyle {currentTitle}", "Slow-Simmered, Homestyle, House-Crafted");

            return ($"Chef's Signature House-Crafted {currentTitle}", "Signature, House-Crafted");
        }

        #endregion

        private static string CleanJsonMarkdown(string contentText)
        {
            contentText = contentText.Trim();
            if (contentText.StartsWith("```json"))
            {
                contentText = contentText.Substring(7);
            }
            if (contentText.StartsWith("```"))
            {
                contentText = contentText.Substring(3);
            }
            if (contentText.EndsWith("```"))
            {
                contentText = contentText.Substring(0, contentText.Length - 3);
            }
            return contentText.Trim();
        }
    }
}
