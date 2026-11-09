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

                if (!hasValidClaudeKey)
                {
                    responseDto.Status = 400;
                    responseDto.ResponseMsg = "AI analysis requires a valid Anthropic (Claude) API key. Please configure the 'Anthropic:ApiKey' setting on the server and try again.";
                    return responseDto;
                }

                // No fallback engine: if Claude is unreachable or rejects the key,
                // its error surfaces to the user through the catch below.
                var report = await CallClaudeFullAnalysisApiAsync(anthropicApiKey!, rawInsights, restaurantName, locationText, dateRangeText, itemContext);

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


        // Audits the restaurant's ACTUAL menu file (PDF or image) with Claude's
        // document/vision reading: layout, placement, prices and descriptions as
        // the guest sees them, checked against the 14 menu-design principles.
        // There is no heuristic fallback for this — a visual audit needs the AI.
        public async Task<ResponseDto> AnalyzeMenuPdfAsync(int currentUserId, MenuPdfAnalysisRequestDto request)
        {
            var responseDto = new ResponseDto();

            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Base64Data))
                {
                    responseDto.Status = 400;
                    responseDto.ResponseMsg = "No menu file was received. Please choose a PDF or an image of your menu.";
                    return responseDto;
                }

                string mediaType = (request.MediaType ?? "").Trim().ToLowerInvariant();
                bool isPdf = mediaType == "application/pdf";
                bool isImage = mediaType == "image/jpeg" || mediaType == "image/png" ||
                               mediaType == "image/webp" || mediaType == "image/gif";
                if (!isPdf && !isImage)
                {
                    responseDto.Status = 400;
                    responseDto.ResponseMsg = "Only PDF files or images (JPG, PNG, WEBP) of the menu are supported.";
                    return responseDto;
                }

                byte[] fileBytes;
                try
                {
                    fileBytes = Convert.FromBase64String(request.Base64Data);
                }
                catch
                {
                    responseDto.Status = 400;
                    responseDto.ResponseMsg = "The uploaded file could not be read. Please try again.";
                    return responseDto;
                }

                if (fileBytes.Length > 12 * 1024 * 1024)
                {
                    responseDto.Status = 400;
                    responseDto.ResponseMsg = "The menu file is too large. Please upload a file under 12 MB.";
                    return responseDto;
                }

                if (isPdf && (fileBytes.Length < 5 || fileBytes[0] != '%' || fileBytes[1] != 'P' || fileBytes[2] != 'D' || fileBytes[3] != 'F'))
                {
                    responseDto.Status = 400;
                    responseDto.ResponseMsg = "The uploaded file does not look like a valid PDF.";
                    return responseDto;
                }

                var anthropicApiKey = _configuration["Anthropic:ApiKey"];
                bool hasValidClaudeKey = !string.IsNullOrWhiteSpace(anthropicApiKey) &&
                                         !anthropicApiKey.Contains("YOUR_CLAUDE_API_KEY", StringComparison.OrdinalIgnoreCase);
                if (!hasValidClaudeKey)
                {
                    responseDto.Status = 400;
                    responseDto.ResponseMsg = "Menu PDF analysis requires the AI service to be configured on the server.";
                    return responseDto;
                }

                var userResponse = await _userService.GetUserAsync(currentUserId);
                var user = userResponse.Data as GetUserDTO;
                var restaurantName = user?.RestaurantName ?? user?.Username ?? "My Restaurant";

                var audit = await CallClaudeMenuPdfAuditAsync(anthropicApiKey!, mediaType, request.Base64Data, restaurantName);
                audit.FileName = request.FileName;
                audit.AnalyzedDate = DateTime.UtcNow.ToString("MMM dd, yyyy HH:mm UTC");

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Menu design audit generated successfully.";
                responseDto.Data = audit;
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = $"Error analyzing menu file: {ex.Message}";
                return responseDto;
            }
        }

        private async Task<MenuPdfAuditDto> CallClaudeMenuPdfAuditAsync(string apiKey, string mediaType, string base64Data, string restaurantName)
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            bool isPdf = mediaType == "application/pdf";
            if (isPdf)
            {
                client.DefaultRequestHeaders.Add("anthropic-beta", "pdfs-2024-09-25");
            }

            var modelName = _configuration["Anthropic:Model"] ?? "claude-3-7-sonnet-20250219";
            int maxTokens = 8192;
            bool disableThinking = false;
            if (modelName.Contains("3-7") || modelName.Contains("3.7"))
            {
                // Claude 3.7 allows far larger outputs under this beta; these
                // reports can exceed 8192 tokens and were being cut off mid-JSON.
                client.DefaultRequestHeaders.Add("anthropic-beta", "output-128k-2025-02-19");
                maxTokens = 16000;
                disableThinking = true;
            }
            else if (!modelName.StartsWith("claude-3"))
            {
                // Newer models (Sonnet 4/5, Opus 4...) think before answering,
                // and the hidden thinking spends the SAME output budget: at
                // 8192 a Sonnet 5 reply once arrived with only a thinking
                // block and no answer text at all. Give real answers room.
                maxTokens = 24000;
                disableThinking = true;
            }

            string systemPrompt = @"You are a world-class menu design auditor for '" + restaurantName + @"'. You are looking at the restaurant's ACTUAL menu — the exact document/image guests see. Audit what is REALLY on the pages: layout and reading flow, where items sit, how prices are printed, description wording and length, colors, boxes/highlights, photos, and white space. Never invent items, prices or sections that are not visible.

Judge the menu against these 14 research-backed menu-design psychology principles (Aaron Allen & Associates, 'The Psychology of Menu Design'):
1. The Golden Triangle — eyes land center first, then top-right, then top-left; the highest-margin items belong in those zones.
2. Feeding the Imagination — specific sensory adjectives ('line-caught', 'sun-dried', 'slow-braised') sell; vague superlatives ('world's best') are ignored.
3. Subconscious Scheming — the first 2 items in each section are ordered most; the last item is the third most profitable slot.
4. Covert Currency — currency signs ($/£) pull attention to cost; prices should be printed as bare numbers.
5. Controlled Costing — charm endings (.95/.50) signal value; round numbers signal premium quality.
6. Limited Choices — about 7 items per section is the comfort zone; more stresses guests and suppresses ordering.
7. A Blast From the Past — nostalgia cues ('Grandma's', 'classic', 'old-fashioned') create comfort and trust.
8. Decoy Dishes — one premium-priced anchor dish makes the rest look like bargains.
9. Storytelling — longer, richer descriptions on high-margin items make them stand out and justify price.
10. Empowering the Customer — helpful information (ingredients, sourcing, spice level) reduces ordering anxiety and lifts orders.
11. Negative Space — white space isolates and spotlights high-value items; crowded menus hide them.
12. Material World — the menu's material/format signals quality (print finish for PDFs, presentation for images).
13. Glossaries — unfamiliar dishes/terms should be briefly explained so guests are not afraid to order them.
14. Color Confusion — green signals freshness, orange stimulates appetite, yellow grabs happy attention, red drives action and sells the highest-margin meals.

OUTPUT RULES:
- Return ONLY a valid JSON object (no markdown, no commentary) in the exact schema shown in the user message.
- Recommendations: return EXACTLY 14 — ONE for EACH principle above, in the same order, using the exact Principle names: 'The Golden Triangle', 'Feeding the Imagination', 'Subconscious Scheming', 'Covert Currency', 'Controlled Costing', 'Limited Choices', 'A Blast From the Past', 'Decoy Dishes', 'Storytelling', 'Empowering the Customer', 'Negative Space', 'Material World', 'Glossaries', 'Color Confusion'. Never skip, merge or repeat a principle. If the menu already does a principle well, say what is working and how to protect/extend it.
- Ground every recommendation in something actually visible on the pages (name the item/section you saw). If a principle cannot be judged from the file, base the recommendation on the closest visible evidence and say so in Reasoning.
- Highlight: for each recommendation, give the approximate area of the page it refers to as percentages of the page size (X, Y = top-left corner; Width, Height = box size; all 0-100) and the 1-based Page number. Keep every Highlight SMALL and specific — one item row, one price column, or one section header; never box a whole section group or most of a page. If the recommendation is about the menu overall rather than one spot, return Highlight as null.
- DesignScore: 0-100 overall menu-design score. Strengths: 2-5 short points. CriticalIssues: 2-5 short points, most damaging first.
- ExtractedMenu: transcribe the ENTIRE visible menu into ExtractedMenu exactly as printed — every section in reading order, every item with its name, its description if one is printed, and its price text exactly as shown (if two prices are printed for one item, join them with ' / '). Never invent items, sections or prices that are not visible.";

            string userText = @"Audit the attached menu file page by page, then return ONLY this JSON:
{
  ""DesignScore"": 0,
  ""Summary"": ""2-3 sentence overall verdict on this menu's design"",
  ""Strengths"": [""...""],
  ""CriticalIssues"": [""...""],
  ""Recommendations"": [
    {
      ""Principle"": ""The Golden Triangle"",
      ""Category"": ""Section name it applies to, or 'Whole Menu'"",
      ""ItemName"": ""Item it applies to, or 'Whole Menu'"",
      ""Recommendation"": ""The exact change to make"",
      ""Reasoning"": ""What you saw on the page + the research logic"",
      ""ExpectedImpact"": ""Expected effect"",
      ""Highlight"": { ""Page"": 1, ""X"": 0, ""Y"": 0, ""Width"": 0, ""Height"": 0 }
    }
  ],
  ""ExtractedMenu"": {
    ""RestaurantName"": ""Restaurant name as printed on the menu, or null"",
    ""Sections"": [
      {
        ""Name"": ""Section name exactly as printed"",
        ""Items"": [
          { ""Name"": ""Item name exactly as printed"", ""Description"": ""Description as printed, or null"", ""PriceText"": ""Price exactly as printed, e.g. 159 or 159 / 219"" }
        ]
      }
    ]
  }
}";

            var fileBlock = new
            {
                type = isPdf ? "document" : "image",
                source = new { type = "base64", media_type = mediaType, data = base64Data }
            };

            var requestBody = new Dictionary<string, object>
            {
                ["model"] = modelName,
                ["max_tokens"] = maxTokens,
                ["system"] = systemPrompt,
                ["messages"] = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[] { fileBlock, new { type = "text", text = userText } }
                    }
                }
            };

            if (disableThinking)
            {
                // Pure-JSON task: thinking would only eat the output budget
                // (and the wait) before any answer text appears.
                requestBody["thinking"] = new { type = "disabled" };
            }
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.anthropic.com/v1/messages", jsonContent);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                string apiError = responseBody.Length > 600 ? responseBody.Substring(0, 600) : responseBody;
                throw new Exception($"Claude API returned {(int)response.StatusCode}: {apiError}");
            }

            using var doc = JsonDocument.Parse(responseBody);

            // Find the first text block — never assume it sits at content[0],
            // and surface the raw response when it has an unexpected shape.
            string contentText = "{}";
            bool foundText = false;
            if (doc.RootElement.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var block in contentArray.EnumerateArray())
                {
                    if (block.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
                    {
                        contentText = textEl.GetString() ?? "{}";
                        foundText = true;
                        break;
                    }
                }
            }

            if (!foundText)
            {
                string raw = responseBody.Length > 600 ? responseBody.Substring(0, 600) : responseBody;
                throw new Exception($"Unexpected response from Claude (no text content): {raw}");
            }

            contentText = CleanJsonMarkdown(contentText);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var audit = JsonSerializer.Deserialize<MenuPdfAuditDto>(contentText, options) ?? new MenuPdfAuditDto();
            audit.SourceModel = $"Claude 3.7 ({modelName})";
            return audit;
        }

        #region Full Analysis (Claude)

        private async Task<MenuAiAnalysisReportDto> CallClaudeFullAnalysisApiAsync(
            string apiKey,
            List<MenuEngineeringInsightsDto> insights,
            string restaurantName,
            string locationText,
            string dateRangeText,
            IReadOnlyDictionary<int, MenuItemContext> itemContext)
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var modelName = _configuration["Anthropic:Model"] ?? "claude-3-7-sonnet-20250219";
            int maxTokens = 8192;
            bool disableThinking = false;
            if (modelName.Contains("3-7") || modelName.Contains("3.7"))
            {
                // Claude 3.7 allows far larger outputs under this beta; these
                // reports can exceed 8192 tokens and were being cut off mid-JSON.
                client.DefaultRequestHeaders.Add("anthropic-beta", "output-128k-2025-02-19");
                maxTokens = 16000;
                disableThinking = true;
            }
            else if (!modelName.StartsWith("claude-3"))
            {
                // Newer models (Sonnet 4/5, Opus 4...) think before answering,
                // and the hidden thinking spends the SAME output budget: at
                // 8192 a Sonnet 5 reply once arrived with only a thinking
                // block and no answer text at all. Give real answers room.
                maxTokens = 24000;
                disableThinking = true;
            }
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

            var requestBody = new Dictionary<string, object>
            {
                ["model"] = modelName,
                ["max_tokens"] = maxTokens,
                ["system"] = systemPrompt,
                ["messages"] = new[] { new { role = "user", content = userPrompt } }
            };

            if (disableThinking)
            {
                // Pure-JSON task: thinking would only eat the output budget
                // (and the wait) before any answer text appears.
                requestBody["thinking"] = new { type = "disabled" };
            }
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.anthropic.com/v1/messages", jsonContent);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                string apiError = responseBody.Length > 600 ? responseBody.Substring(0, 600) : responseBody;
                throw new Exception($"Claude API returned {(int)response.StatusCode}: {apiError}");
            }

            using var doc = JsonDocument.Parse(responseBody);

            // Find the first text block — never assume it sits at content[0]:
            // a non-text first block (or an unexpected response shape) used to
            // throw "The given key was not present in the dictionary" here.
            string contentText = "{}";
            bool foundText = false;
            if (doc.RootElement.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var block in contentArray.EnumerateArray())
                {
                    if (block.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
                    {
                        contentText = textEl.GetString() ?? "{}";
                        foundText = true;
                        break;
                    }
                }
            }

            if (!foundText)
            {
                string stopReason = doc.RootElement.TryGetProperty("stop_reason", out var srEl) && srEl.ValueKind == JsonValueKind.String ? srEl.GetString() ?? "unknown" : "unknown";
                string raw = responseBody.Length > 600 ? responseBody.Substring(0, 600) : responseBody;
                throw new Exception($"Unexpected response from Claude (no text content, stop_reason: {stopReason}): {raw}");
            }

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


        // Builds a design-ready menu (sections + items + selling prices) from
        // the restaurant's own MenuOptics data — used when the user does NOT
        // upload a menu file and still wants a fresh premium menu design.
        public async Task<ResponseDto> GetMenuDesignDataAsync(int currentUserId)
        {
            var responseDto = new ResponseDto();

            try
            {
                var userResponse = await _userService.GetUserAsync(currentUserId);
                var user = userResponse.Data as GetUserDTO;
                var restaurantName = user?.RestaurantName ?? user?.Username ?? "My Restaurant";

                var menuResponse = await _menuService.GetAllMenuItemsAsync(null, currentUserId);
                var menuItems = (menuResponse.Data as IEnumerable<GetMenuItemDTO>)?.ToList() ?? new List<GetMenuItemDTO>();

                if (!menuItems.Any())
                {
                    responseDto.Status = 404;
                    responseDto.ResponseMsg = "No menu items found. Please add menu items first, then generate a design.";
                    return responseDto;
                }

                var menu = new ExtractedMenuDto
                {
                    RestaurantName = restaurantName,
                    Sections = menuItems
                        .GroupBy(i => string.IsNullOrWhiteSpace(i.CategoryName) ? "Menu" : i.CategoryName)
                        .Select(g => new ExtractedMenuSectionDto
                        {
                            Name = g.Key,
                            Items = g.Select(i => new ExtractedMenuItemDto
                            {
                                Name = i.Name,
                                Description = i.Description,
                                PriceText = i.SellingPrice > 0 ? i.SellingPrice.ToString("0.00") : "",
                                IngredientsUsed = (i.Ingredients ?? new List<GetMenuItemIngredientDTO>())
                                    .Select(x => (x.IngredientName ?? "").Trim())
                                    .Where(x => x.Length > 0)
                                    .Concat((i.Recipes ?? new List<GetMenuItemRecipeDTO>())
                                        .Select(x => (x.RecipeName ?? "").Trim())
                                        .Where(x => x.Length > 0))
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .Take(8)
                                    .ToList()
                            }).ToList()
                        })
                        .ToList()
                };

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Menu data loaded successfully.";
                responseDto.Data = menu;
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = $"Error loading menu data: {ex.Message}";
                return responseDto;
            }
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

        // Menu-design psychology (Aaron Allen — 'The Psychology of Menu Design'), rule-based edition.

        #endregion

        public async Task<ResponseDto> GenerateMenuDesignsAsync(ExtractedMenuDto menu)
        {
            var responseDto = new ResponseDto();
            try
            {
                if (menu == null || menu.Sections == null || !menu.Sections.Any(s => s.Items != null && s.Items.Any()))
                {
                    responseDto.Status = 400;
                    responseDto.ResponseMsg = "No menu items were provided for the design.";
                    return responseDto;
                }

                var anthropicApiKey = _configuration["Anthropic:ApiKey"];
                bool hasValidClaudeKey = !string.IsNullOrWhiteSpace(anthropicApiKey) &&
                                         !anthropicApiKey.Contains("YOUR_CLAUDE_API_KEY", StringComparison.OrdinalIgnoreCase);
                if (!hasValidClaudeKey)
                {
                    responseDto.Status = 400;
                    responseDto.ResponseMsg = "AI design variations require the AI service to be configured on the server.";
                    return responseDto;
                }

                var result = await CallClaudeDesignVariationsAsync(anthropicApiKey!, menu);
                if (result == null || result.Designs.Count == 0)
                {
                    responseDto.Status = 400;
                    responseDto.ResponseMsg = "The AI could not create design variations this time. Please try again.";
                    return responseDto;
                }

                responseDto.Status = 200;
                responseDto.ResponseMsg = "AI design variations generated successfully.";
                responseDto.Data = result;
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = ex.Message;
                return responseDto;
            }
        }

        private async Task<AiMenuDesignsResultDto> CallClaudeDesignVariationsAsync(string apiKey, ExtractedMenuDto menu)
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var modelName = _configuration["Anthropic:Model"] ?? "claude-3-7-sonnet-20250219";

            // A compact picture of the menu for the designer: section names + a few item names.
            var menuDigest = new StringBuilder();
            menuDigest.AppendLine("Restaurant: " + (string.IsNullOrWhiteSpace(menu.RestaurantName) ? "Our Restaurant" : menu.RestaurantName));
            foreach (var sec in menu.Sections ?? new List<ExtractedMenuSectionDto>())
            {
                var items = sec.Items ?? new List<ExtractedMenuItemDto>();
                var sample = string.Join(", ", items.Take(6).Select(i => i.Name));
                menuDigest.AppendLine($"- {sec.Name} ({items.Count} items): {sample}");
            }

            string systemPrompt = @"You are an award-winning print menu designer. You design the LOOK of restaurant menus — palettes, typography and layout. You never change menu content.

Create EXACTLY 4 design concepts for the given restaurant menu. The 4 concepts must be GENUINELY different from each other in mood and appearance (for example: dark luxury, bright modern, rustic warm, minimal elegant — adapt the moods to this restaurant's cuisine and item names). Never return 4 variations of the same idea.

Rules for every concept:
- Palette: full 6-digit hex colors. Bg = sheet background, Ink = main text, Sub = secondary/description text, Accent = headings and highlights, Price = price color, Divider = dotted-leader / panel-line color. GUARANTEE readability: Ink must contrast strongly with Bg (light text on a dark Bg or dark text on a light Bg); Sub and Price must also stay clearly readable on Bg.
- Fonts: choose HeadFont and BodyFont ONLY from: Georgia, Times New Roman, Palatino, Garamond, Segoe UI, Trebuchet MS, Arial Black, Courier New, Lucida Handwriting, Brush Script, Verdana.
- Frame: one of none, single, double. Align: one of center, left. PriceStyle: one of dots (dotted leader between item and price), plain (price on the right, no dots), below (price printed under the item name).
- Ornament: one decorative character from: ✦ ❦ ★ ◆ ❧ — or an empty string for a clean look.
- Name: a 2-3 word concept name (e.g. 'Midnight Vineyard'). Tagline: one short phrase describing the mood.
- Menu title: also create SuggestedTitle — a short, attractive title for this menu itself (2-5 words) that fits its dishes, and SuggestedTagline — one short inviting line for the menu (max 8 words). If a real restaurant name is given, base the title on it; otherwise invent a fitting name from the dishes.

Return ONLY a valid JSON object (no markdown, no commentary):
{ ""SuggestedTitle"": """", ""SuggestedTagline"": """", ""designs"": [ { ""Name"": """", ""Tagline"": """", ""Bg"": ""#000000"", ""Ink"": ""#000000"", ""Sub"": ""#000000"", ""Accent"": ""#000000"", ""Price"": ""#000000"", ""Divider"": ""#000000"", ""HeadFont"": ""Georgia"", ""BodyFont"": ""Georgia"", ""Frame"": ""none"", ""Align"": ""center"", ""PriceStyle"": ""dots"", ""Ornament"": """" } ] }";

            var requestBody = new
            {
                model = modelName,
                max_tokens = 2000,
                system = systemPrompt,
                messages = new[]
                {
                    new { role = "user", content = "Design 4 concepts for this menu:\n" + menuDigest.ToString() }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.anthropic.com/v1/messages", jsonContent);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                string apiError = responseBody.Length > 600 ? responseBody.Substring(0, 600) : responseBody;
                throw new Exception($"Claude API returned {(int)response.StatusCode}: {apiError}");
            }

            using var doc = JsonDocument.Parse(responseBody);

            string contentText = "{}";
            bool foundText = false;
            if (doc.RootElement.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var block in contentArray.EnumerateArray())
                {
                    if (block.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
                    {
                        contentText = textEl.GetString() ?? "{}";
                        foundText = true;
                        break;
                    }
                }
            }

            if (!foundText)
            {
                string raw = responseBody.Length > 600 ? responseBody.Substring(0, 600) : responseBody;
                throw new Exception($"Unexpected response from Claude (no text content): {raw}");
            }

            contentText = CleanJsonMarkdown(contentText);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsed = JsonSerializer.Deserialize<AiDesignsResponse>(contentText, options) ?? new AiDesignsResponse();
            var title = ClampText(parsed.SuggestedTitle, "", 60);
            var tagline = ClampText(parsed.SuggestedTagline, "", 90);
            return new AiMenuDesignsResultDto
            {
                Designs = (parsed.Designs ?? new List<AiMenuDesignSpecDto>())
                    .Take(4)
                    .Select(SanitizeDesignSpec)
                    .ToList(),
                SuggestedTitle = title.Length == 0 ? null : title,
                SuggestedTagline = tagline.Length == 0 ? null : tagline
            };
        }

        private class AiDesignsResponse
        {
            public List<AiMenuDesignSpecDto>? Designs { get; set; }
            public string? SuggestedTitle { get; set; }
            public string? SuggestedTagline { get; set; }
        }

        private static readonly string[] DesignFonts = { "Georgia", "Times New Roman", "Palatino", "Garamond", "Segoe UI", "Trebuchet MS", "Arial Black", "Courier New", "Lucida Handwriting", "Brush Script", "Verdana" };
        private static readonly string[] DesignOrnaments = { "✦", "❦", "★", "◆", "❧", "" };

        // The AI proposes; the system disposes. Anything outside the allowed sets is
        // clamped to a safe default, and low-contrast text colors are corrected so a
        // returned design can never render unreadable.
        private static AiMenuDesignSpecDto SanitizeDesignSpec(AiMenuDesignSpecDto spec)
        {
            spec ??= new AiMenuDesignSpecDto();
            spec.Name = ClampText(spec.Name, "AI Design", 40);
            spec.Tagline = ClampText(spec.Tagline, "", 90);
            spec.Bg = ValidHex(spec.Bg, "#ffffff");
            spec.Ink = ValidHex(spec.Ink, "#1f2937");
            spec.Sub = ValidHex(spec.Sub, "#6b7280");
            spec.Accent = ValidHex(spec.Accent, "#f08d11");
            spec.Price = ValidHex(spec.Price, spec.Accent);
            spec.Divider = ValidHex(spec.Divider, "#d1d5db");
            spec.HeadFont = DesignFonts.Contains(spec.HeadFont) ? spec.HeadFont : "Georgia";
            spec.BodyFont = DesignFonts.Contains(spec.BodyFont) ? spec.BodyFont : "Segoe UI";
            spec.Frame = (spec.Frame == "single" || spec.Frame == "double") ? spec.Frame : "none";
            spec.Align = spec.Align == "left" ? "left" : "center";
            spec.PriceStyle = (spec.PriceStyle == "plain" || spec.PriceStyle == "below") ? spec.PriceStyle : "dots";
            spec.Ornament = DesignOrnaments.Contains(spec.Ornament ?? "") ? spec.Ornament : "";

            if (ContrastRatio(spec.Bg, spec.Ink) < 3.0)
                spec.Ink = Luminance(spec.Bg) > 0.5 ? "#111111" : "#ffffff";
            if (ContrastRatio(spec.Bg, spec.Sub) < 2.2)
                spec.Sub = spec.Ink;
            if (ContrastRatio(spec.Bg, spec.Price) < 2.2)
                spec.Price = spec.Accent;
            return spec;
        }

        // The guarantee behind every direction line: if the text does not name
        // at least two of the dish's ingredients (or its only ingredient), the
        // server weaves them in itself — presence never depends on the AI alone.
        private static string EnsureIngredientsNamed(string direction, List<string> used)
        {
            if (used == null || used.Count == 0) return direction;
            var named = used.Count(u => direction.Contains(u, StringComparison.OrdinalIgnoreCase));
            var needed = Math.Min(2, used.Count);
            if (named >= needed) return direction;
            var withList = string.Join(", ", used);
            return direction.Length == 0
                ? $"Prepared with {withList}."
                : $"{direction.TrimEnd('.', ' ')} — with {withList}.";
        }

        private static string ClampText(string? value, string fallback, int max)
        {
            var v = (value ?? "").Trim();
            if (v.Length == 0) return fallback;
            return v.Length > max ? v.Substring(0, max) : v;
        }

        private static string ValidHex(string? value, string fallback)
        {
            var v = (value ?? "").Trim();
            if (v.Length == 7 && v[0] == '#' && v.Skip(1).All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))) return v;
            return fallback;
        }

        private static double Luminance(string hex)
        {
            double Channel(int i)
            {
                var c = Convert.ToInt32(hex.Substring(i, 2), 16) / 255.0;
                return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * Channel(1) + 0.7152 * Channel(3) + 0.0722 * Channel(5);
        }

        private static double ContrastRatio(string a, string b)
        {
            var la = Luminance(a);
            var lb = Luminance(b);
            var hi = Math.Max(la, lb);
            var lo = Math.Min(la, lb);
            return (hi + 0.05) / (lo + 0.05);
        }

        public async Task<ResponseDto> GenerateMenuFromIngredientsAsync(IngredientMenuRequestDto request)
        {
            var responseDto = new ResponseDto();
            try
            {
                var totalIngredients = request?.Sections?.Sum(s => s.Ingredients?.Count(i => !string.IsNullOrWhiteSpace(i)) ?? 0) ?? 0;
                if (request == null || totalIngredients == 0)
                {
                    responseDto.Status = 400;
                    responseDto.ResponseMsg = "Please add at least one ingredient.";
                    return responseDto;
                }

                var anthropicApiKey = _configuration["Anthropic:ApiKey"];
                bool hasValidClaudeKey = !string.IsNullOrWhiteSpace(anthropicApiKey) &&
                                         !anthropicApiKey.Contains("YOUR_CLAUDE_API_KEY", StringComparison.OrdinalIgnoreCase);
                if (!hasValidClaudeKey)
                {
                    responseDto.Status = 400;
                    responseDto.ResponseMsg = "Creating a menu from ingredients requires the AI service to be configured on the server.";
                    return responseDto;
                }

                var menu = await CallClaudeIngredientMenuAsync(anthropicApiKey!, request);
                if (menu == null || menu.Sections == null || !menu.Sections.Any(s => s.Items != null && s.Items.Any()))
                {
                    responseDto.Status = 400;
                    responseDto.ResponseMsg = "The AI could not create a menu from these ingredients this time. Please try again.";
                    return responseDto;
                }

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Menu created from your ingredients.";
                responseDto.Data = menu;
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = ex.Message;
                return responseDto;
            }
        }

        private async Task<ExtractedMenuDto> CallClaudeIngredientMenuAsync(string apiKey, IngredientMenuRequestDto request)
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var modelName = _configuration["Anthropic:Model"] ?? "claude-3-7-sonnet-20250219";

            var digest = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(request.RestaurantName))
                digest.AppendLine("Restaurant: " + request.RestaurantName.Trim());
            foreach (var sec in request.Sections ?? new List<IngredientSectionInputDto>())
            {
                var ings = (sec.Ingredients ?? new List<string>())
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Select(i => i.Trim())
                    .ToList();
                if (ings.Count == 0) continue;
                digest.AppendLine($"Group '{sec.Name}': " + string.Join(", ", ings));
            }

            string systemPrompt = @"You are an executive chef and menu consultant. A restaurant gives you the INGREDIENTS it has in its kitchen, in groups. Your job: invent a complete, sellable menu of DISHES that can be cooked from those ingredients.

Rules:
- Every dish's main ingredients must come from the provided lists. Basic pantry staples (salt, pepper, cooking oil, flour, sugar, water) may be assumed, but never build a dish around a main ingredient that was not provided.
- Organize the dishes into clear menu sections (for example Starters, Mains, Grill, Sides, Desserts, Drinks) — use only sections that fit the ingredients. If an ingredient group has a meaningful name, its dishes may sit under that name.
- Create 3-6 dishes per ingredient group. Each dish: an appealing name, a realistic price as a plain number with NO currency symbol (e.g. 249 or 12.50), and a Description written as a DIRECTION — one flowing line that tells how the dish is prepared, using its ingredients with cooking verbs and the cooking sequence, in exactly this style: 'bistro shrimp sautéed in garlic butter and white wine, then baked with mozzarella cheese and garlic bread'. The direction MUST explicitly name at least two of the dish's own ingredients. Just the direction line — no label, no bullet points.
- IngredientsUsed (per dish): the dish's main ingredients, copied from the provided lists — names only, without any amounts.
- RestaurantName: use the restaurant name if one is given; otherwise invent a short, fitting name for the place based on the ingredients.
- Tagline: one short inviting line for under the menu name, built around 2-3 of the star ingredients provided (e.g. 'Built around fresh shrimp, garlic and mozzarella').
- Return ONLY a valid JSON object (no markdown, no commentary) in exactly this shape:
{ ""RestaurantName"": """", ""Tagline"": """", ""Sections"": [ { ""Name"": """", ""Items"": [ { ""Name"": """", ""Description"": """", ""PriceText"": """", ""IngredientsUsed"": [""""] } ] } ] }";

            var requestBody = new
            {
                model = modelName,
                max_tokens = 4096,
                system = systemPrompt,
                messages = new[]
                {
                    new { role = "user", content = "Here are the ingredients in our kitchen:\n" + digest.ToString() }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.anthropic.com/v1/messages", jsonContent);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                string apiError = responseBody.Length > 600 ? responseBody.Substring(0, 600) : responseBody;
                throw new Exception($"Claude API returned {(int)response.StatusCode}: {apiError}");
            }

            using var doc = JsonDocument.Parse(responseBody);

            string contentText = "{}";
            bool foundText = false;
            if (doc.RootElement.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var block in contentArray.EnumerateArray())
                {
                    if (block.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
                    {
                        contentText = textEl.GetString() ?? "{}";
                        foundText = true;
                        break;
                    }
                }
            }

            if (!foundText)
            {
                string raw = responseBody.Length > 600 ? responseBody.Substring(0, 600) : responseBody;
                throw new Exception($"Unexpected response from Claude (no text content): {raw}");
            }

            contentText = CleanJsonMarkdown(contentText);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var menu = JsonSerializer.Deserialize<ExtractedMenuDto>(contentText, options) ?? new ExtractedMenuDto();
            return SanitizeIngredientMenu(menu, request);
        }

        private static ExtractedMenuDto SanitizeIngredientMenu(ExtractedMenuDto menu, IngredientMenuRequestDto request)
        {
            menu ??= new ExtractedMenuDto();
            menu.RestaurantName = !string.IsNullOrWhiteSpace(request.RestaurantName)
                ? request.RestaurantName.Trim()
                : (string.IsNullOrWhiteSpace(menu.RestaurantName) ? "Our Restaurant" : menu.RestaurantName.Trim());
            var tagline = ClampText(menu.Tagline, "", 90);
            menu.Tagline = tagline.Length == 0 ? null : tagline;

            // The pool of ingredient names the user actually provided (amounts stripped).
            var pool = new List<string>();
            foreach (var sec in request.Sections ?? new List<IngredientSectionInputDto>())
            {
                foreach (var raw in sec.Ingredients ?? new List<string>())
                {
                    var name = (raw ?? "").Trim();
                    var paren = name.IndexOf('(');
                    if (paren > 0) name = name.Substring(0, paren).Trim();
                    if (name.Length > 0 && !pool.Contains(name, StringComparer.OrdinalIgnoreCase)) pool.Add(name);
                }
            }

            var sections = new List<ExtractedMenuSectionDto>();
            foreach (var sec in (menu.Sections ?? new List<ExtractedMenuSectionDto>()).Take(10))
            {
                var items = new List<ExtractedMenuItemDto>();
                foreach (var i in (sec.Items ?? new List<ExtractedMenuItemDto>()).Where(x => !string.IsNullOrWhiteSpace(x.Name)).Take(14))
                {
                    // Keep only ingredients that truly came from the user's lists.
                    var used = (i.IngredientsUsed ?? new List<string>())
                        .Select(u => (u ?? "").Trim())
                        .Where(u => u.Length > 0 && pool.Contains(u, StringComparer.OrdinalIgnoreCase))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(6)
                        .ToList();

                    var direction = string.IsNullOrWhiteSpace(i.Description) ? "" : i.Description.Trim();

                    // Guarantee: the direction must name the dish's ingredients.
                    direction = EnsureIngredientsNamed(direction, used);

                    items.Add(new ExtractedMenuItemDto
                    {
                        Name = i.Name.Trim(),
                        Description = direction.Length == 0 ? null : direction,
                        PriceText = (i.PriceText ?? "").Trim(),
                        IngredientsUsed = used
                    });
                }
                if (items.Count == 0) continue;
                sections.Add(new ExtractedMenuSectionDto
                {
                    Name = string.IsNullOrWhiteSpace(sec.Name) ? "Menu" : sec.Name.Trim(),
                    Items = items
                });
            }
            menu.Sections = sections;
            return menu;
        }

        // Rewrites the under-item text of a design menu as guest-facing DIRECTION
        // lines, written by Claude from each dish's own recipe ingredients. The
        // saved item descriptions in MenuOptics are internal kitchen prep notes
        // (timings, plating) — they are never shown on a designed menu as-is.
        public async Task<ResponseDto> GenerateMenuDirectionsAsync(ExtractedMenuDto menu)
        {
            var responseDto = new ResponseDto();
            try
            {
                if (menu == null || menu.Sections == null || !menu.Sections.Any(s => s.Items != null && s.Items.Any()))
                {
                    responseDto.Status = 400;
                    responseDto.ResponseMsg = "No menu items were provided.";
                    return responseDto;
                }

                var anthropicApiKey = _configuration["Anthropic:ApiKey"];
                bool hasValidClaudeKey = !string.IsNullOrWhiteSpace(anthropicApiKey) &&
                                         !anthropicApiKey.Contains("YOUR_CLAUDE_API_KEY", StringComparison.OrdinalIgnoreCase);
                if (!hasValidClaudeKey)
                {
                    responseDto.Status = 400;
                    responseDto.ResponseMsg = "Writing menu directions requires a valid Anthropic (Claude) API key. Please configure the 'Anthropic:ApiKey' setting on the server and try again.";
                    return responseDto;
                }

                await CallClaudeMenuDirectionsAsync(anthropicApiKey!, menu);

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Menu directions written successfully.";
                responseDto.Data = menu;
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = ex.Message;
                return responseDto;
            }
        }

        private async Task CallClaudeMenuDirectionsAsync(string apiKey, ExtractedMenuDto menu)
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var modelName = _configuration["Anthropic:Model"] ?? "claude-3-7-sonnet-20250219";

            // Flatten the dishes in order; directions come back against these indexes.
            var flat = new List<(ExtractedMenuItemDto Item, string Section)>();
            foreach (var sec in menu.Sections ?? new List<ExtractedMenuSectionDto>())
                foreach (var item in sec.Items ?? new List<ExtractedMenuItemDto>())
                    flat.Add((item, sec.Name ?? "Menu"));
            var work = flat.Take(80).ToList();

            var digest = new StringBuilder();
            for (int n = 0; n < work.Count; n++)
            {
                var (item, section) = work[n];
                var ings = item.IngredientsUsed != null && item.IngredientsUsed.Count > 0
                    ? string.Join(", ", item.IngredientsUsed)
                    : "(not listed)";
                var current = (item.Description ?? "").Trim();
                if (current.Length > 220) current = current.Substring(0, 220);
                digest.AppendLine($"{n + 1}. [{section}] {item.Name} | ingredients: {ings} | kitchen notes: {current}");
            }

            string systemPrompt = @"You are a menu copywriter for restaurants. For every dish in the list, write its DIRECTION — the one flowing line printed under the dish name on the guest menu.

Style — exactly like this example: 'bistro shrimp sautéed in garlic butter and white wine, then baked with mozzarella cheese and garlic bread'.

Rules:
- One line per dish, about 15-35 words, appetizing, written for guests: how the dish is prepared, with cooking verbs and the cooking sequence.
- The line MUST name the dish's key ingredients: use its ingredient list when one is given (name at least two of them); when no list is given, name the ingredients the dish is defined by (the ones in its name and kind).
- The 'kitchen notes' are INTERNAL prep instructions — mine them only for true facts about the dish (real ingredients, real cooking methods). NEVER copy their style: no equipment timings ('microwave for 1 min', 'oven for 4 mins'), no plating or station instructions ('transfer to a rectangular plate', 'place filled bowl'), no abbreviations or shorthand.
- Do not invent a different dish, and do not add a main ingredient the dish does not have.
- Return ONLY a valid JSON object (no markdown, no commentary), one entry per dish with the SAME Index numbers as the input:
{ ""Directions"": [ { ""Index"": 1, ""Direction"": """" } ] }";

            var requestBody = new
            {
                model = modelName,
                max_tokens = 6144,
                system = systemPrompt,
                messages = new[]
                {
                    new { role = "user", content = "Write a direction for each dish:\n" + digest.ToString() }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.anthropic.com/v1/messages", jsonContent);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                string apiError = responseBody.Length > 600 ? responseBody.Substring(0, 600) : responseBody;
                throw new Exception($"Claude API returned {(int)response.StatusCode}: {apiError}");
            }

            using var doc = JsonDocument.Parse(responseBody);

            string contentText = "{}";
            bool foundText = false;
            if (doc.RootElement.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var block in contentArray.EnumerateArray())
                {
                    if (block.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
                    {
                        contentText = textEl.GetString() ?? "{}";
                        foundText = true;
                        break;
                    }
                }
            }

            if (!foundText)
            {
                string raw = responseBody.Length > 600 ? responseBody.Substring(0, 600) : responseBody;
                throw new Exception($"Unexpected response from Claude (no text content): {raw}");
            }

            contentText = CleanJsonMarkdown(contentText);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsed = JsonSerializer.Deserialize<MenuDirectionsResponse>(contentText, options) ?? new MenuDirectionsResponse();

            var byIndex = new Dictionary<int, string>();
            foreach (var d in parsed.Directions ?? new List<MenuDirectionEntry>())
            {
                var text = (d.Direction ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
                if (text.Length > 320) text = text.Substring(0, 320).Trim();
                if (text.Length > 0) byIndex[d.Index] = text;
            }

            for (int n = 0; n < work.Count; n++)
            {
                var (item, _) = work[n];
                if (byIndex.TryGetValue(n + 1, out var direction))
                {
                    // Same guarantee as the AI-chef flow: the ingredients must
                    // actually appear in the line, woven in by the server if not.
                    item.Description = EnsureIngredientsNamed(direction, item.IngredientsUsed ?? new List<string>());
                }
            }
        }

        private class MenuDirectionsResponse
        {
            public List<MenuDirectionEntry>? Directions { get; set; }
        }

        private class MenuDirectionEntry
        {
            public int Index { get; set; }
            public string? Direction { get; set; }
        }

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
