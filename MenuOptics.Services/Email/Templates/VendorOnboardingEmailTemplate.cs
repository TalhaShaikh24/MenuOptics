using System.Net;

namespace MenuOptics.Services.EmailTemplates
{
    public static class VendorOnboardingEmailTemplate
    {
        public static string BuildSubject(string? businessName)
        {
            var name = string.IsNullOrWhiteSpace(businessName) ? "Customer" : businessName.Trim();
            return $"New Customer Onboarding: {name} – Menu Optics Setup";
        }

        public static string BuildHtml(string? tmName, string? restaurantOrCustomerName, string? vendorAccountName, string baseUrl = "")
        {
            var safeTmName = string.IsNullOrWhiteSpace(tmName) ? "Territory Manager" : WebUtility.HtmlEncode(tmName);
            var safeCustomerName = string.IsNullOrWhiteSpace(restaurantOrCustomerName) ? "Customer" : WebUtility.HtmlEncode(restaurantOrCustomerName);
            var safeAccountName = string.IsNullOrWhiteSpace(vendorAccountName) ? "Account on file" : WebUtility.HtmlEncode(vendorAccountName);

            var logoUrl = string.IsNullOrWhiteSpace(baseUrl)
                ? "https://menuoptics.com/images/menu%20optics_logo.png"
                : $"{baseUrl.TrimEnd('/')}/images/menu%20optics_logo.png";

            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>New Customer Onboarding - Menu Optics</title>
  <style>
    @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap');
    * {{ margin: 0; padding: 0; box-sizing: border-box; }}
    body {{
      background-color: #f8fafc;
      font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      color: #1f2937;
      -webkit-font-smoothing: antialiased;
      padding: 24px 12px;
    }}
    .email-wrapper {{
      max-width: 620px;
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
      border-radius: 14px;
      padding: 10px 22px;
      margin-bottom: 18px;
      box-shadow: 0 2px 10px rgba(0,0,0,0.12);
    }}
    .header-logo-wrap img {{
      height: 38px;
      width: auto;
      display: block;
    }}
    .header h1 {{
      font-size: 24px;
      font-weight: 800;
      color: #ffffff;
      margin-bottom: 6px;
      letter-spacing: -0.3px;
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
    }}
    .body {{
      padding: 32px 36px;
    }}
    .greeting {{
      font-size: 17px;
      font-weight: 700;
      color: #111827;
      margin-bottom: 16px;
    }}
    .lead-text {{
      font-size: 15px;
      color: #374151;
      line-height: 1.65;
      margin-bottom: 22px;
    }}
    .customer-card {{
      background: #f9fafb;
      border: 1px solid #e5e7eb;
      border-radius: 12px;
      padding: 18px 20px;
      margin-bottom: 22px;
    }}
    .customer-card table {{
      width: 100%;
      border-collapse: collapse;
    }}
    .customer-card td {{
      padding: 6px 0;
      font-size: 14px;
    }}
    .card-label {{
      color: #6b7280;
      font-weight: 600;
      width: 42%;
    }}
    .card-value {{
      color: #111827;
      font-weight: 600;
    }}
    .sop-callout {{
      background: #fff7ed;
      border: 1.5px solid #fed7aa;
      border-left: 5px solid #f08d11;
      border-radius: 8px;
      padding: 16px 18px;
      margin-bottom: 22px;
      font-size: 14px;
      color: #7c2d12;
      line-height: 1.6;
    }}
    .sop-callout strong {{
      color: #9a3412;
      display: block;
      margin-bottom: 4px;
      font-size: 14.5px;
    }}
    .closing {{
      font-size: 14.5px;
      color: #4b5563;
      line-height: 1.7;
      margin-top: 24px;
    }}
    .footer {{
      background: #f9fafb;
      padding: 20px 36px;
      border-top: 1px solid #e5e7eb;
      text-align: center;
      font-size: 12.5px;
      color: #6b7280;
      line-height: 1.6;
    }}
    .footer a {{
      color: #f08d11;
      text-decoration: none;
      font-weight: 600;
    }}
  </style>
</head>
<body>
  <div class=""email-wrapper"">
    <div class=""header"">
      <div class=""header-logo-wrap"">
        <img src=""{logoUrl}"" alt=""Menu Optics Logo"" />
      </div>
      <h1>New Customer Onboarding</h1>
      <span class=""badge"">Live Setup &amp; Data Integration</span>
    </div>

    <div class=""body"">
      <div class=""greeting"">Hi {safeTmName},</div>

      <p class=""lead-text"">
        We are pleased to let you know that your customer, <strong>{safeCustomerName} ({safeAccountName})</strong>, has officially signed up and activated their subscription with Menu Optics!
      </p>

      <div class=""customer-card"">
        <table>
          <tr>
            <td class=""card-label"">Customer / Restaurant:</td>
            <td class=""card-value"">{safeCustomerName}</td>
          </tr>
          <tr>
            <td class=""card-label"">Vendor Account Number:</td>
            <td class=""card-value"">{safeAccountName}</td>
          </tr>
          <tr>
            <td class=""card-label"">Subscription Status:</td>
            <td class=""card-value"" style=""color:#16a34a;"">&#10004; Active &amp; Verified</td>
          </tr>
        </table>
      </div>

      <p class=""lead-text"">
        To ensure their live catalogue pricing and electronic invoice data flow seamlessly into their new profile, please follow the steps outlined in our <strong>Menu Optics Onboarding SOP</strong>.
      </p>

      <div class=""sop-callout"">
        <strong>&#128206; Onboarding SOP Document Attached</strong>
        For your convenience, we have attached a copy of the SOP directly to this email. Following this procedure ensures their account mapping is completed accurately and allows automated data syncing to begin without delay.
      </div>

      <p class=""lead-text"">
        If you have any questions or need any assistance during the onboarding process, please don’t hesitate to reach out to us at <a href=""mailto:support@menuoptics.com"" style=""color:#f08d11;font-weight:600;text-decoration:none;"">support@menuoptics.com</a>.
      </p>

      <p class=""lead-text"" style=""margin-bottom:12px;"">
        Thank you for your continued partnership and support in helping our mutual customers gain total control over their food costs and margins.
      </p>

      <div class=""closing"">
        Best regards,<br />
        <strong style=""color:#111827;font-size:15px;"">Menu Optics</strong><br />
        <a href=""mailto:support@menuoptics.com"" style=""color:#f08d11;text-decoration:none;font-weight:600;"">support@menuoptics.com</a><br />
        <a href=""https://www.menuoptics.com"" style=""color:#f08d11;text-decoration:none;"">www.menuoptics.com</a>
      </div>
    </div>

    <div class=""footer"">
      <p>
        &copy; {DateTime.UtcNow.Year} Menu Optics. All rights reserved.<br />
        Real-Time Food Costing &amp; AI Menu Engineering Platform
      </p>
    </div>
  </div>
</body>
</html>";
        }
    }
}
