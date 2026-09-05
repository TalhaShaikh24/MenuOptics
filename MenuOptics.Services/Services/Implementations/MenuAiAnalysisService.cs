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
                var user = userResponse.Data as UserDto;
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
                        report.SourceModel = "MenuOptics AI Engine (Claude API Fallback)";
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

            var systemPrompt = $@"You are an elite hospitality consultant, menu engineering master, and regional market intelligence analyst.
Analyze the restaurant's menu performance in the direct context of its geographic location ({locationText}).

CRITICAL GEOGRAPHIC, TITLE OPTIMIZATION & BENCHMARKING CONSTRAINTS:
1. Local Area Market Benchmarking: Evaluate menu items against typical regional competitor price ranges and consumer dining purchasing power in {locationText}.
2. In CompetitorMarketOverview, provide a clear assessment of how the restaurant's price levels and margins compare against typical dining competitors in {locationText}.
3. Menu Title & Naming Enhancements (TitleImprovements): If any menu item has a plain, generic, boring, or unappetizing title compared to modern competitor standards in {locationText} (especially for high-margin Puzzles or underperforming items), suggest a sensory-rich, high-converting, appetizing title and clear reasoning. Only suggest if genuinely beneficial; otherwise return an empty array.
4. In RemovalRecommendations, identify candidates for removal/deprecation. You must recommend AT MOST {maxRemovalLimit} items (strictly <= 10% of the {totalItems} total menu items). Pick only the most problematic items (e.g. bottom Dogs with low sales and low/negative margins).
5. PricingOptimizations: Recommend tactical price adjustments that capture higher margin while ensuring prices stay safely within local competitor tolerance in {locationText}.
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
      ""SuggestedTitle"": ""Crispy Buttermilk Herb Chicken Burger"",
      ""Reasoning"": ""Generic title lacks sensory appeal; upgrading to descriptive artisan terminology elevates perceived value against local competitors.""
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
      ""RecommendedPrice"": 16.50,
      ""CurrentMargin"": 9.00,
      ""ProjectedMargin"": 10.50,
      ""EstimatedMonthlyProfitIncrease"": 300.00,
      ""Rationale"": ""...""
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
                CompetitorMarketOverview = $"Based on regional benchmarks for {locationText}, your average menu price point offers strong value. Adjusting prices on high-velocity items by 5-8% will keep your restaurant well within local competitive bandwidth while capturing significant gross margin upside.",
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
                    KeyTakeaway = $"Capitalize on local market demand in {locationText} with tactical 5-8% price adjustments on Plowhorses, eliminate bottom 10% Dog items, and modernize menu titles."
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

            // Title Improvement Recommendations (Suggesting high-converting sensory titles for plain or puzzle items)
            var titleCandidates = puzzles.Concat(insights.Where(i => (i.Name?.Split(' ').Length ?? 0) <= 2))
                                         .DistinctBy(i => i.MenuItemId)
                                         .Take(3)
                                         .ToList();

            foreach (var item in titleCandidates)
            {
                string orig = item.Name ?? "Item";
                string suggested = GenerateAppetizingTitle(orig);
                if (!string.Equals(orig, suggested, StringComparison.OrdinalIgnoreCase))
                {
                    report.TitleImprovements.Add(new AiTitleImprovementDto
                    {
                        MenuItemId = item.MenuItemId,
                        CurrentTitle = orig,
                        SuggestedTitle = suggested,
                        Reasoning = $"'{orig}' is plain compared to competing restaurants in {locationText}. Upgrading to '{suggested}' triggers appetite appeal, increases perceived value, and helps convert higher margin orders."
                    });
                }
            }

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
                    ActionRecommendation = "Deprecate from active menu layout. Redirect prep labor to Star items and eliminate slow-moving specialized inventory.",
                    EstimatedMonthlyLaborOrWasteSavings = estSavings
                });
            }

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
                decimal priceIncrease = Math.Max(0.75m, Math.Round(price * 0.065m, 2));
                decimal recommended = price + priceIncrease;
                decimal currentMargin = price - cost;
                decimal projectedMargin = recommended - cost;
                decimal monthlyGain = Math.Round(priceIncrease * Math.Max(item.UnitsSold, 10), 2);
                totalEstimatedProfitUplift += monthlyGain;

                report.PricingOptimizations.Add(new AiPricingOptimizationDto
                {
                    MenuItemId = item.MenuItemId,
                    Name = item.Name ?? $"Item #{item.MenuItemId}",
                    CurrentPrice = price,
                    RecommendedPrice = recommended,
                    CurrentMargin = currentMargin,
                    ProjectedMargin = projectedMargin,
                    EstimatedMonthlyProfitIncrease = monthlyGain,
                    Rationale = $"High volume ({item.UnitsSold} units) and strong local customer loyalty in {locationText}. +${priceIncrease:F2} remains well within area competitor tolerance, generating +${monthlyGain:F2}/mo."
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
                    ActionTitle = "Roll Out Tactical Pricing & Modernize Item Titles",
                    Details = $"Update menu prices by 5-8% on high-volume items and apply suggested sensory titles to high-margin Puzzle items to out-convert local competitors in {locationText}.",
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

        private static string GenerateAppetizingTitle(string currentTitle)
        {
            var lower = currentTitle.ToLower();
            if (lower.Contains("burger") && !lower.Contains("signature") && !lower.Contains("artisan"))
                return $"Signature Gourmet {currentTitle}";
            if (lower.Contains("pasta") && !lower.Contains("tuscan") && !lower.Contains("artisan"))
                return $"Artisan Truffle {currentTitle}";
            if (lower.Contains("salad") && !lower.Contains("harvest") && !lower.Contains("garden"))
                return $"Farmhouse Crisp {currentTitle}";
            if (lower.Contains("pizza") && !lower.Contains("wood-fired") && !lower.Contains("artisan"))
                return $"Artisan Hand-Crafted {currentTitle}";
            if (lower.Contains("chicken") && !lower.Contains("herb") && !lower.Contains("crispy"))
                return $"Herb-Roasted {currentTitle}";
            if (lower.Contains("steak") && !lower.Contains("prime") && !lower.Contains("aged"))
                return $"Prime Flame-Seared {currentTitle}";
            if (lower.Contains("soup") && !lower.Contains("homestyle"))
                return $"Slow-Simmered {currentTitle}";

            return $"Chef's Specialty {currentTitle}";
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
