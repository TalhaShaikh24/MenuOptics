using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using MenuOptics.Models.DTOs;
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
        private readonly IMenuService _menuService;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public MenuAiAnalysisService(
            IMenuEngineeringInsightsService insightsService,
            IUserService userService,
            IMenuService menuService,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _insightsService = insightsService;
            _userService = userService;
            _menuService = menuService;
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

                // Menu context (category + current description per item) — powers the
                // menu-design psychology advice. Never blocks the analysis if unavailable.
                var itemContext = await GetMenuItemContextAsync(currentUserId);

                var anthropicApiKey = _configuration["Anthropic:ApiKey"];
                bool hasValidClaudeKey = !string.IsNullOrWhiteSpace(anthropicApiKey) &&
                                         !anthropicApiKey.Contains("YOUR_CLAUDE_API_KEY", StringComparison.OrdinalIgnoreCase);

                MenuAiAnalysisReportDto report;

                if (hasValidClaudeKey)
                {
                    try
                    {
                        report = await CallClaudeFullAnalysisApiAsync(anthropicApiKey!, rawInsights, restaurantName, locationText, dateRangeText, itemContext);
                    }
                    catch
                    {
                        report = GenerateHeuristicFullAiAnalysis(rawInsights, restaurantName, locationText, dateRangeText, itemContext);
                        report.SourceModel = "MenuOptics AI Engine";
                    }
                }
                else
                {
                    report = GenerateHeuristicFullAiAnalysis(rawInsights, restaurantName, locationText, dateRangeText, itemContext);
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
            string dateRangeText,
            IReadOnlyDictionary<int, MenuItemContext> itemContext)
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
                Category = GetCategory(itemContext, i.MenuItemId),
                Description = GetDescription(itemContext, i.MenuItemId),
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
6. MenuPlanningChecklist: Convert this report's own recommendations (removals, pricing, titles, and all 14 design principles) into a practical MENU PLANNING CHECKLIST the operator can tick off one by one. Each entry is ONE concrete task in imperative form ('Remove ...', 'Change the price of ...', 'Move ...'), naming the exact item/section and the exact change, with a short Details note and an Area ('Removals', 'Pricing', 'Titles & Descriptions', or 'Menu Design'). NO dates, NO phases, NO timeframes — it is a checklist, not a roadmap. Order: removals first, then pricing, then titles & descriptions, then menu design.
7. Menu-Design Psychology (Aaron Allen & Associates — 'The Psychology of Menu Design'): Apply ALL of the following in MenuDesignRecommendations, and let them inform TitleImprovements and the MenuPlanningChecklist. Name the principle in each recommendation's Principle field.
   - The Golden Triangle: A guest's eyes move to the CENTER of the menu first, then the TOP-RIGHT, then the TOP-LEFT. Recommend the highest-margin Stars/Puzzles for these three eye-magnet zones.
   - First-Two & Last-Item Rule (Subconscious Scheming): Guests subconsciously order the TOP TWO items of each menu section most often, and a distinct segment always picks the LAST item. Per section (use the Category field): positions 1-2 must hold the section's highest-margin items, and the last position its third most profitable item. Flag sections wasting these slots on low-margin items.
   - Decoy Dishes (Price Anchoring): A slightly more expensive item at the top of a section makes every other dish look like better value and lifts spend. Per major section, confirm an effective anchor exists or nominate a decoy candidate priced ~15-25% above the section's target item, and name the item it anchors.
   - Covert Currency: Currency signs are the guest's biggest pain point — they pull attention to cost. Recommend printing prices WITHOUT currency symbols (bare numbers). Where the concept allows, prices written out in words can lift spend by up to 30%; flag it as an optional premium tactic.
   - Limited Choices (Rule of Seven): More than ~7 items per section stresses guests and suppresses ordering. Flag every section with more than 7 items and say how many to trim (respect the 10% removal cap — prefer repositioning/merging over removal where possible).
   - Feeding the Imagination: Descriptions must use enticing, specific sensory adjectives ('line-caught', 'sun-dried', 'slow-braised') that make taste buds react. Superlative claims ('world's best', 'finest ever') are ignored by guests — never suggest them; call them out when an item relies on one.
   - Storytelling (Description Length): When most descriptions are a similar length, a LONGER description breaks the pattern and catches the eye. Recommend extended storytelling descriptions ONLY for the highest-margin items the restaurant most wants to sell; use the Description field to judge which items currently have thin descriptions.
   - Nostalgia ('A Blast From the Past'): Emotionally-worded, nostalgic naming ('Grandma's Apple Pie', 'Nonna's Sunday Sauce') creates resonance that is hard to resist. Where a dish genuinely fits (comfort food, desserts, heritage recipes), suggest a nostalgic angle in TitleImprovements and say so in the Reasoning.
   - Negative Space: On a crowded menu the eye is drawn to open space. Recommend isolating the single highest-margin item of each major section in its own visual space (box, border, or whitespace) away from the dense listings.
   - Color Psychology: Colors steer ordering — GREEN signals freshness, ORANGE stimulates appetite, YELLOW is a happy attention-grabber, RED encourages action and sells the highest-margin meals. Recommend orange/red accent highlighting for items placed in Golden Triangle zones; green accents for fresh/health-positioned items.
   - Empowering the Customer: The more information a guest has about an item, the more likely they are to choose it. Recommend richer detail (origin, preparation, pairing notes) for premium and slow-moving items instead of stripping detail back.
   - Glossaries: Guests order an unfamiliar, pricey item (e.g., steak tartare) more readily when they know exactly how it is prepared and produced. For items with unfamiliar or intimidating names, recommend a short glossary note or one-line preparation explainer instead of renaming the dish away.
   - Material World (print menus only): Menu material signals quality — premium concepts should use heavy stock / leather-style presentation, value concepts a clean simple format. Include at most one menu-level recommendation on this, and only when it fits the concept.
8. MenuDesignRecommendations Output: Return EXACTLY 14 recommendations — ONE for EACH research principle, in this exact order, using these exact Principle names: 'The Golden Triangle', 'Feeding the Imagination', 'Subconscious Scheming', 'Covert Currency', 'Controlled Costing', 'Limited Choices', 'A Blast From the Past', 'Decoy Dishes', 'Storytelling', 'Empowering the Customer', 'Negative Space', 'Material World', 'Glossaries', 'Color Confusion'. Every principle MUST appear exactly once — never skip, merge, or repeat one. Ground each recommendation in THIS menu's data (real items, categories and prices from the dataset); when a principle has no single obvious item, give the advice at section or menu level rather than dropping it. Item-level entries must use the real MenuItemId and Name; section-level entries set Category; menu-level entries leave MenuItemId, ItemName and Category null.
9. Return ONLY a valid JSON object matching the requested schema. No markdown wrapping, no extra prose.";

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
  ""MenuPlanningChecklist"": [
    {{
      ""Task"": ""Remove '...' from the menu"",
      ""Details"": ""..."",
      ""Area"": ""Removals""
    }},
    {{
      ""Task"": ""Change the price of '...' from $0.00 to $0.00"",
      ""Details"": ""..."",
      ""Area"": ""Pricing""
    }}
  ],
  ""MenuDesignRecommendations"": [
    {{
      ""Principle"": ""Golden Triangle Placement"",
      ""Category"": ""Mains"",
      ""MenuItemId"": 7,
      ""ItemName"": ""Sea Bass in White Wine Sauce"",
      ""Recommendation"": ""Move this item to the top-right zone of the Mains section — the second position every guest's eyes land on after the center."",
      ""Reasoning"": ""It is the section's highest-margin Star ($18.40 contribution) but sits mid-list where scan-path attention is weakest."",
      ""ExpectedImpact"": ""Higher order share for the top-margin main within 2-4 weeks.""
    }},
    {{
      ""Principle"": ""Covert Currency"",
      ""Category"": null,
      ""MenuItemId"": null,
      ""ItemName"": null,
      ""Recommendation"": ""Print all prices as bare numbers without the currency symbol (e.g., '15.95' instead of '$15.95')."",
      ""Reasoning"": ""Currency symbols are the guest's biggest pain point and pull attention straight to cost; removing them is the cheapest change in the research."",
      ""ExpectedImpact"": ""Menu-wide reduction in price resistance.""
    }}
  ]
}}";

            var requestBody = new
            {
                model = modelName,
                max_tokens = 8192,
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
            string dateRangeText,
            IReadOnlyDictionary<int, MenuItemContext> itemContext)
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

            AddMenuDesignRecommendations(report, insights, itemContext);
            AddMenuPlanningChecklist(report);

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

        private async Task<IReadOnlyDictionary<int, MenuItemContext>> GetMenuItemContextAsync(int currentUserId)
        {
            var map = new Dictionary<int, MenuItemContext>();
            try
            {
                var menuResponse = await _menuService.GetAllMenuItemsAsync(null, currentUserId);
                var menuItems = (menuResponse.Data as IEnumerable<GetMenuItemDTO>)?.ToList() ?? new List<GetMenuItemDTO>();
                foreach (var item in menuItems)
                {
                    map[item.MenuItemId] = new MenuItemContext
                    {
                        Category = string.IsNullOrWhiteSpace(item.CategoryName) ? "General" : item.CategoryName,
                        Description = item.Description
                    };
                }
            }
            catch
            {
                // Category/description context only enhances the advice — never block the analysis.
            }
            return map;
        }

        private static string GetCategory(IReadOnlyDictionary<int, MenuItemContext> itemContext, int menuItemId)
            => itemContext.TryGetValue(menuItemId, out var ctx) && !string.IsNullOrWhiteSpace(ctx.Category) ? ctx.Category : "General";

        private static string? GetDescription(IReadOnlyDictionary<int, MenuItemContext> itemContext, int menuItemId)
            => itemContext.TryGetValue(menuItemId, out var ctx) ? ctx.Description : null;

        private sealed class MenuItemContext
        {
            public string Category { get; set; } = "General";
            public string? Description { get; set; }
        }

        // Section 7: the menu planning checklist — every recommendation in this
        // report restated as one tick-off task. Not a roadmap: no dates or phases.
        private static void AddMenuPlanningChecklist(MenuAiAnalysisReportDto report)
        {
            var items = new List<AiChecklistItemDto>();

            foreach (var r in report.RemovalRecommendations)
            {
                items.Add(new AiChecklistItemDto
                {
                    Area = "Removals",
                    Task = $"Remove '{r.Name}' from the menu",
                    Details = r.ActionRecommendation
                });
            }

            foreach (var pr in report.PricingOptimizations)
            {
                items.Add(new AiChecklistItemDto
                {
                    Area = "Pricing",
                    Task = $"Change the price of '{pr.Name}' from ${pr.CurrentPrice:F2} to ${pr.RecommendedPrice:F2}",
                    Details = pr.PricingPrinciple
                });
            }

            foreach (var t in report.TitleImprovements)
            {
                items.Add(new AiChecklistItemDto
                {
                    Area = "Titles & Descriptions",
                    Task = $"Rename '{t.CurrentTitle}' to '{t.SuggestedTitle}'",
                    Details = t.TrendVerbiageUsed
                });
            }

            foreach (var d in report.MenuDesignRecommendations)
            {
                items.Add(new AiChecklistItemDto
                {
                    Area = "Menu Design",
                    Task = d.Recommendation,
                    Details = d.Principle
                });
            }

            report.MenuPlanningChecklist = items;
        }

        // Menu-design psychology (Aaron Allen — 'The Psychology of Menu Design'), rule-based edition.
        // Emits exactly ONE recommendation per research principle (all 14, in research order),
        // so the report's Menu Design & Psychology section is complete in fallback mode too.
        private static void AddMenuDesignRecommendations(
            MenuAiAnalysisReportDto report,
            List<MenuEngineeringInsightsDto> insights,
            IReadOnlyDictionary<int, MenuItemContext> itemContext)
        {
            var recs = new List<AiMenuDesignRecommendationDto>();

            string CategoryOf(MenuEngineeringInsightsDto item) => GetCategory(itemContext, item.MenuItemId);

            var heroItem = insights
                .Where(i => string.Equals(i.MenuQuadrant, "Star", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i => i.ContributionMargin)
                .FirstOrDefault() ?? insights.OrderByDescending(i => i.ContributionMargin).First();
            var topPuzzle = insights
                .Where(i => string.Equals(i.MenuQuadrant, "Puzzle", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i => i.ContributionMargin)
                .FirstOrDefault();
            var priciest = insights.OrderByDescending(i => i.SellingPrice ?? 0).First();
            var biggestSection = insights.GroupBy(CategoryOf).OrderByDescending(g => g.Count()).First();
            decimal avgPrice = insights.Average(i => i.SellingPrice ?? 0);

            // 1. The Golden Triangle
            recs.Add(new AiMenuDesignRecommendationDto
            {
                Principle = "The Golden Triangle",
                Category = CategoryOf(heroItem),
                MenuItemId = heroItem.MenuItemId,
                ItemName = heroItem.Name,
                Recommendation = $"Place '{heroItem.Name}' in the center or top-right zone of the {CategoryOf(heroItem)} section — the first positions a guest's eyes land on.",
                Reasoning = $"It is the menu's highest-margin hero item (${heroItem.ContributionMargin:F2} contribution per sale). Research shows the eye travels center -> top-right -> top-left, and those zones should hold the dishes the restaurant most wants to sell.",
                ExpectedImpact = "Higher order share for the top-margin item."
            });

            // 2. Feeding the Imagination
            var blandItem = insights
                .Where(i => (i.Name ?? "").Trim().Split(' ').Length <= 2)
                .OrderByDescending(i => i.ContributionMargin)
                .FirstOrDefault() ?? heroItem;
            recs.Add(new AiMenuDesignRecommendationDto
            {
                Principle = "Feeding the Imagination",
                Category = CategoryOf(blandItem),
                MenuItemId = blandItem.MenuItemId,
                ItemName = blandItem.Name,
                Recommendation = $"Rewrite the description of '{blandItem.Name}' with specific, enticing sensory words ('line-caught', 'sun-dried', 'slow-braised') that make taste buds react.",
                Reasoning = "Descriptions that feed the imagination sell the dish before it arrives. Superlative claims like 'world's best' are simply ignored by guests — concrete sensory adjectives are not.",
                ExpectedImpact = "Higher conversion on a plainly-presented item."
            });

            // 3. Subconscious Scheming (first-two & last-item rule, shown on the biggest section)
            var sectionByMargin = biggestSection.OrderByDescending(i => i.ContributionMargin).ToList();
            var first = sectionByMargin[0];
            var second = sectionByMargin.ElementAtOrDefault(1);
            var third = sectionByMargin.ElementAtOrDefault(2);
            string topTwo = second != null ? $"'{first.Name}' and '{second.Name}'" : $"'{first.Name}'";
            string lastSlot = third != null ? $" and close the section with '{third.Name}' (its third most profitable dish)" : "";
            recs.Add(new AiMenuDesignRecommendationDto
            {
                Principle = "Subconscious Scheming",
                Category = biggestSection.Key,
                Recommendation = $"Reorder the {biggestSection.Key} section: put {topTwo} in positions 1-2{lastSlot}. Apply the same pattern in every section.",
                Reasoning = "Guests subconsciously order the top two items of each section more often, and a distinct segment always picks the last item — so positions 1-2 belong to the highest-margin dishes and the last slot to the third most profitable one.",
                ExpectedImpact = "More orders steered to high-margin dishes in every section."
            });

            // 4. Covert Currency
            recs.Add(new AiMenuDesignRecommendationDto
            {
                Principle = "Covert Currency",
                Recommendation = "Print prices as bare numbers without the currency symbol (e.g., '15.95' instead of '$15.95') across the whole menu.",
                Reasoning = "Paying is the guest's biggest pain point, and currency signs pull attention straight to cost. Removing them is the cheapest change in the research; prices written out in words can lift spend by up to 30% where the concept allows.",
                ExpectedImpact = "Menu-wide reduction in price resistance."
            });

            // 5. Controlled Costing
            recs.Add(new AiMenuDesignRecommendationDto
            {
                Principle = "Controlled Costing",
                Recommendation = "Use charm endings (.95 / .50) for mainstream items so prices feel like a good deal, and clean round numbers for premium signature items to signal quality.",
                Reasoning = $"A price like $10.95 feels like a bargain, while round numbers add an air of chic sophistication at the top end. With an average menu price of ${avgPrice:F2}, keep flagship items like '{priciest.Name}' (${(priciest.SellingPrice ?? 0):F2}) on confident, round price points.",
                ExpectedImpact = "Better perceived value at every price tier."
            });

            // 6. Limited Choices (Rule of Seven)
            var oversized = insights.GroupBy(CategoryOf).Where(g => g.Count() > 7).OrderByDescending(g => g.Count()).FirstOrDefault();
            if (oversized != null)
            {
                recs.Add(new AiMenuDesignRecommendationDto
                {
                    Principle = "Limited Choices",
                    Category = oversized.Key,
                    Recommendation = $"Trim '{oversized.Key}' from {oversized.Count()} items toward 7 — start with its lowest-margin, lowest-selling items (see Removal Recommendations).",
                    Reasoning = "A huge section forces guests to choose between too many options and stresses them; around seven dishes per section feels like plenty without overwhelming, and ordering rises.",
                    ExpectedImpact = "Faster decisions and a higher order rate in this section."
                });
            }
            else
            {
                recs.Add(new AiMenuDesignRecommendationDto
                {
                    Principle = "Limited Choices",
                    Category = biggestSection.Key,
                    Recommendation = $"Every section is currently within the ideal range — the largest, '{biggestSection.Key}', has {biggestSection.Count()} items. Keep it that way: when adding a dish, retire or merge one.",
                    Reasoning = "Around seven dishes per section gives guests plenty of choice without the stress that suppresses ordering; this menu already respects that balance.",
                    ExpectedImpact = "Protects the current healthy order rate as the menu evolves."
                });
            }

            // 7. A Blast From the Past (Nostalgia)
            var nostalgiaWords = new[] { "pie", "cake", "pudding", "soup", "stew", "roast", "classic", "homestyle", "grandma" };
            var nostalgiaItem = insights.FirstOrDefault(i => nostalgiaWords.Any(w => (i.Name ?? "").Contains(w, StringComparison.OrdinalIgnoreCase)))
                ?? insights.FirstOrDefault(i => CategoryOf(i).Contains("Dessert", StringComparison.OrdinalIgnoreCase));
            if (nostalgiaItem != null)
            {
                recs.Add(new AiMenuDesignRecommendationDto
                {
                    Principle = "A Blast From the Past",
                    Category = CategoryOf(nostalgiaItem),
                    MenuItemId = nostalgiaItem.MenuItemId,
                    ItemName = nostalgiaItem.Name,
                    Recommendation = $"Give '{nostalgiaItem.Name}' a nostalgic, emotionally-worded name and description (the 'Grandma's Apple Pie' treatment) that loads it with memories of home cooking.",
                    Reasoning = "Nostalgia is a powerful force: a carefully worded description gives a dish an emotional resonance that is hard to resist, especially for comfort food and desserts.",
                    ExpectedImpact = "Stronger emotional pull and order rate for this item."
                });
            }
            else
            {
                recs.Add(new AiMenuDesignRecommendationDto
                {
                    Principle = "A Blast From the Past",
                    Recommendation = "Add one nostalgic, heritage-style dish (or rename a comfort-food item) with emotionally-worded copy — the 'Grandma's Apple Pie' treatment.",
                    Reasoning = "Nostalgia is a powerful force: carefully worded descriptions give a dish an emotional resonance that is hard to resist, and this menu currently has no item playing that role.",
                    ExpectedImpact = "A new emotional anchor item guests remember and return for."
                });
            }

            // 8. Decoy Dishes
            var anchor = biggestSection.OrderByDescending(i => i.SellingPrice ?? 0).First();
            var decoyTarget = biggestSection.Where(i => i.MenuItemId != anchor.MenuItemId)
                                            .OrderByDescending(i => i.ContributionMargin)
                                            .FirstOrDefault();
            if (decoyTarget != null)
            {
                recs.Add(new AiMenuDesignRecommendationDto
                {
                    Principle = "Decoy Dishes",
                    Category = biggestSection.Key,
                    MenuItemId = anchor.MenuItemId,
                    ItemName = anchor.Name,
                    Recommendation = $"List '{anchor.Name}' (${(anchor.SellingPrice ?? 0):F2}) at the top of {biggestSection.Key} and keep it undiscounted, so '{decoyTarget.Name}' reads as the smart-value choice next to it.",
                    Reasoning = "A slightly more expensive item at the top of a section makes every other dish appear to offer more for the money — and guests who feel they are getting a bargain spend more.",
                    ExpectedImpact = $"Stronger mix toward '{decoyTarget.Name}' and other mid-range {biggestSection.Key} items."
                });
            }
            else
            {
                recs.Add(new AiMenuDesignRecommendationDto
                {
                    Principle = "Decoy Dishes",
                    Category = biggestSection.Key,
                    MenuItemId = priciest.MenuItemId,
                    ItemName = priciest.Name,
                    Recommendation = $"Use '{priciest.Name}' (${(priciest.SellingPrice ?? 0):F2}) as the price anchor at the top of its section and keep it undiscounted.",
                    Reasoning = "A slightly more expensive item at the top makes every other dish appear to offer more for the money — and guests who feel they are getting a bargain spend more.",
                    ExpectedImpact = "Higher spend on the mid-range items below the anchor."
                });
            }

            // 9. Storytelling
            recs.Add(new AiMenuDesignRecommendationDto
            {
                Principle = "Storytelling",
                Category = CategoryOf(heroItem),
                MenuItemId = heroItem.MenuItemId,
                ItemName = heroItem.Name,
                Recommendation = $"Write a noticeably longer description for '{heroItem.Name}' than for its neighbours — origin, preparation, and one memorable detail.",
                Reasoning = "Most descriptions sit at a similar length, so the one that breaks the pattern sticks out and catches the eye; the longest descriptions belong on the items with the highest profit margins.",
                ExpectedImpact = "More attention and orders for the highest-margin item."
            });

            // 10. Empowering the Customer
            var empowerItem = topPuzzle ?? priciest;
            recs.Add(new AiMenuDesignRecommendationDto
            {
                Principle = "Empowering the Customer",
                Category = CategoryOf(empowerItem),
                MenuItemId = empowerItem.MenuItemId,
                ItemName = empowerItem.Name,
                Recommendation = $"Add richer information for '{empowerItem.Name}' — how it is prepared, where its key ingredient comes from, and what to pair it with.",
                Reasoning = "The more information listed about an item, the more confident and empowered guests feel to choose it — this is why detailed wine lists sell more wine. High-margin items guests hesitate over gain the most.",
                ExpectedImpact = "Higher order rate for a high-margin item guests currently pass over."
            });

            // 11. Negative Space
            recs.Add(new AiMenuDesignRecommendationDto
            {
                Principle = "Negative Space",
                Category = CategoryOf(heroItem),
                MenuItemId = heroItem.MenuItemId,
                ItemName = heroItem.Name,
                Recommendation = $"Set '{heroItem.Name}' in its own visual space (a box or border with breathing room), away from the dense run of descriptions around it.",
                Reasoning = "When a menu is crammed with text, the eye is naturally drawn to open space — items with the largest margins are therefore set apart from the hustle and bustle of the other listings.",
                ExpectedImpact = "The hero item is noticed first, at no cost."
            });

            // 12. Material World
            bool premiumLeaning = avgPrice >= 15;
            recs.Add(new AiMenuDesignRecommendationDto
            {
                Principle = "Material World",
                Recommendation = premiumLeaning
                    ? "For print menus, use a premium physical format (heavy stock, leather-style cover) that matches this menu's price level."
                    : "For print menus, use a clean, simple, well-kept physical format that signals honest value for money.",
                Reasoning = $"The material of the menu communicates the brand image: high-end restaurants use leather and thick paper to suggest food of similar quality, while a value concept should look clean and straightforward. This menu's average price is ${avgPrice:F2}.",
                ExpectedImpact = "Menu presentation that matches — and justifies — the price level."
            });

            // 13. Glossaries
            recs.Add(new AiMenuDesignRecommendationDto
            {
                Principle = "Glossaries",
                Category = CategoryOf(priciest),
                MenuItemId = priciest.MenuItemId,
                ItemName = priciest.Name,
                Recommendation = $"Add a short glossary note or one-line preparation explainer for '{priciest.Name}' and any other unfamiliar specialty terms on the menu.",
                Reasoning = "Guests are far more likely to order a pricey, unfamiliar item (the classic example is steak tartare) when they know exactly how it is prepared and produced, instead of feeling intimidated by the name.",
                ExpectedImpact = "Fewer skipped premium items; higher premium mix."
            });

            // 14. Color Confusion
            recs.Add(new AiMenuDesignRecommendationDto
            {
                Principle = "Color Confusion",
                Category = CategoryOf(heroItem),
                MenuItemId = heroItem.MenuItemId,
                ItemName = heroItem.Name,
                Recommendation = $"Highlight '{heroItem.Name}' with an orange or red accent; use green accents on fresh items and yellow sparingly where the eye should land.",
                Reasoning = "Colors affect what we order: green implies the food is fresh, orange stimulates the appetite, yellow is a happy hue that catches attention, and red encourages action — which is why it is used to sell the meals with the highest profit margins.",
                ExpectedImpact = "Color quietly steers orders toward the highest-margin items."
            });

            report.MenuDesignRecommendations = recs;
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
