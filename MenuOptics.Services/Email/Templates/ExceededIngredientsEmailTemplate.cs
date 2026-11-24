using System;
using System.Collections.Generic;
using System.Net;
using MenuOptics.Models.DTOs;

namespace MenuOptics.Services.EmailTemplates
{
    public static class ExceededIngredientsEmailTemplate
    {
        public static string Build(
            string? restaurantName,
            decimal thresholdPercentage,
            List<IngredientPriceComparisonDTO> exceededIngredients,
            string baseUrl = "")
        {
            var restaurant = string.IsNullOrWhiteSpace(restaurantName) ? "Valued Restaurant" : WebUtility.HtmlEncode(restaurantName);
            var today = DateTime.UtcNow;
            var formattedDate = today.ToString("dddd, MMMM d, yyyy");

            var logoUrl = string.IsNullOrWhiteSpace(baseUrl)
                ? "https://menuoptics.com/images/menu%20optics_logo.png"
                : $"{baseUrl.TrimEnd('/')}/images/menu%20optics_logo.png";

            var dashboardUrl = string.IsNullOrWhiteSpace(baseUrl)
                ? "https://menuoptics.com/Ingredient"
                : $"{baseUrl.TrimEnd('/')}/Ingredient";

            var rowsHtml = string.Empty;
            foreach (var item in exceededIngredients)
            {
                var name = WebUtility.HtmlEncode(item.IngredientName);
                var isInc = item.IsPriceIncrease;
                var color = isInc ? "#dc2626" : "#16a34a";
                var sign = isInc ? "+" : "-";

                rowsHtml += $@"
                <tr>
                  <td style=""padding: 12px 14px; border-bottom: 1px solid #e5e7eb; vertical-align: top;"">
                    <div style=""font-weight: 700; color: #111827; font-size: 13.5px;"">{name}</div>
                  </td>
                  <td style=""padding: 12px 14px; border-bottom: 1px solid #e5e7eb; text-align: right; color: #111827; vertical-align: top; font-size: 13px;"">{item.AffectedMenuItem}</td>
                  <td style=""padding: 12px 14px; border-bottom: 1px solid #e5e7eb; text-align: right; color: #6b7280; vertical-align: top; font-size: 13px;"">${item.OldPrice:F2}</td>
                  <td style=""padding: 12px 14px; border-bottom: 1px solid #e5e7eb; text-align: right; font-weight: 700; color: #111827; vertical-align: top; font-size: 13px;"">${item.CurrentPrice:F2}</td>
                  <td style=""padding: 12px 14px; border-bottom: 1px solid #e5e7eb; text-align: right; color: {color}; font-weight: 600; vertical-align: top; font-size: 13px;"">
                    {sign}${Math.Abs(item.PriceDifference):F2}
                  </td>
                  <td style=""padding: 12px 14px; border-bottom: 1px solid #e5e7eb; text-align: right; vertical-align: top;"">
                    <span style=""display: inline-block; background: {(isInc ? "#fee2e2" : "#dcfce7")}; color: {color}; padding: 3px 10px; border-radius: 12px; font-weight: 700; font-size: 13px;"">
                      {sign}{item.PercentageChange:F1}%
                    </span>
                  </td>
                </tr>";
            }

            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Ingredient Price Alert - Menu Optics</title>
  <style>
    @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap');
    * {{ margin: 0; padding: 0; box-sizing: border-box; }}
    body {{
      background-color: #f8fafc;
      font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      color: #1f2937;
      padding: 30px 12px;
    }}
    .email-wrapper {{
      max-width: 650px;
      margin: 0 auto;
      background: #ffffff;
      border-radius: 16px;
      overflow: hidden;
      box-shadow: 0 4px 24px rgba(0,0,0,0.08);
      border: 1px solid #e5e7eb;
    }}
    .header {{
      background: linear-gradient(135deg, #f08d11 0%, #ff9f2e 100%);
      padding: 32px 36px 28px;
      text-align: center;
    }}
    .header-logo-wrap {{
      display: inline-block;
      background: #ffffff;
      border-radius: 12px;
      padding: 8px 20px;
      margin-bottom: 14px;
      box-shadow: 0 2px 10px rgba(0,0,0,0.12);
    }}
    .header-logo-wrap img {{
      height: 36px;
      width: auto;
      display: block;
    }}
    .header h1 {{
      font-size: 22px;
      font-weight: 800;
      color: #ffffff;
      margin-bottom: 4px;
    }}
    .badge {{
      display: inline-block;
      background: rgba(255,255,255,0.25);
      border: 1px solid rgba(255,255,255,0.45);
      border-radius: 20px;
      padding: 4px 14px;
      font-size: 13px;
      font-weight: 600;
      color: #ffffff;
      margin-top: 4px;
    }}
    .body {{
      padding: 32px 36px;
    }}
    .lead-text {{
      font-size: 15px;
      color: #374151;
      line-height: 1.6;
      margin-bottom: 20px;
    }}
    .threshold-badge {{
      display: inline-block;
      background: #fffbeb;
      border: 1px solid #fde68a;
      color: #92400e;
      padding: 6px 14px;
      border-radius: 8px;
      font-weight: 700;
      font-size: 14px;
      margin-bottom: 20px;
    }}
    .table-container {{
      width: 100%;
      border-collapse: collapse;
      border: 1px solid #e5e7eb;
      border-radius: 10px;
      overflow: hidden;
      margin-bottom: 24px;
      font-size: 13px;
    }}
    .table-container th {{
      background: #f3f4f6;
      padding: 12px 14px;
      font-size: 12px;
      font-weight: 700;
      color: #4b5563;
      text-transform: uppercase;
      letter-spacing: 0.3px;
    }}
    .btn-wrap {{
      text-align: center;
      margin: 28px 0 10px;
    }}
    .btn-primary {{
      display: inline-block;
      background: #f08d11;
      color: #ffffff !important;
      font-size: 14px;
      font-weight: 700;
      padding: 12px 28px;
      border-radius: 8px;
      text-decoration: none;
    }}
    .footer {{
      background: #f9fafb;
      border-top: 1px solid #e5e7eb;
      padding: 20px 36px;
      text-align: center;
      font-size: 12px;
      color: #9ca3af;
      line-height: 1.6;
    }}
  </style>
</head>
<body>
  <div class=""email-wrapper"">
    <div class=""header"">
      <div class=""header-logo-wrap"">
        <img src=""{logoUrl}"" alt=""Menu Optics"" />
      </div>
      <h1>Ingredient Price Change Alert</h1>
      <span class=""badge"">{formattedDate}</span>
    </div>

    <div class=""body"">
      <p class=""lead-text"">
        Hello <strong>{restaurant}</strong>,<br />
        The following <strong>{exceededIngredients.Count} ingredient(s)</strong> have changed price and met or exceeded your alert threshold:
      </p>

      <div class=""threshold-badge"">
        ⚙️ Configured Alert Threshold: <strong>{thresholdPercentage:F1}%</strong>
      </div>

      <table class=""table-container"" cellpadding=""0"" cellspacing=""0"">
        <thead>
          <tr>
            <th style=""text-align: left;"">Ingredient</th>
            <th style=""text-align: left;"">Affected Menu Item</th>
            <th style=""text-align: right;"">Old Price</th>
            <th style=""text-align: right;"">Current Price</th>
            <th style=""text-align: right;"">Difference</th>
            <th style=""text-align: right;"">% Change</th>
          </tr>
        </thead>
        <tbody>
          {rowsHtml}
        </tbody>
      </table>

      <div class=""btn-wrap"">
        <a href=""{dashboardUrl}"" class=""btn-primary"" target=""_blank"">View Ingredients in Menu Optics</a>
      </div>
    </div>

    <div class=""footer"">
      <p>This automated price alert was generated based on your restaurant's configured threshold.</p>
      <p>&copy; {DateTime.UtcNow.Year} Menu Optics. All rights reserved.</p>
    </div>
  </div>
</body>
</html>";
        }
    }
}
