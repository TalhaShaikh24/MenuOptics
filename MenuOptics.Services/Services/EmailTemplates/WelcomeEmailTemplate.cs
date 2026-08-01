using System.Net;

namespace MenuOptics.Services.EmailTemplates
{
    public static class WelcomeEmailTemplate
    {
        public static string Build(string? firstName, string? restaurantName, string baseUrl = "", string email = "")
        {
            var restaurant = string.IsNullOrWhiteSpace(restaurantName) ? "your restaurant" : restaurantName;

            // Build current date string: e.g. "Wednesday, August 1st"
            var today = DateTime.Now;
            var dayName = today.ToString("dddd");          // e.g. Wednesday
            var monthName = today.ToString("MMMM");        // e.g. August
            var day = today.Day;
            var suffix = day switch
            {
                1 or 21 or 31 => "st",
                2 or 22        => "nd",
                3 or 23        => "rd",
                _              => "th"
            };
            var currentDate = $"{dayName}, {monthName} {day}{suffix}";

            // Logo URL – from WebUrl (appsettings) + static path
            var logoUrl = string.IsNullOrWhiteSpace(baseUrl)
                ? "https://menuoptics.com/images/menu%20optics_logo.png"
                : $"{baseUrl.TrimEnd('/')}/images/menu%20optics_logo.png";

            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Welcome to Menu Optics</title>
  <style>
    @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap');

    * {{ margin: 0; padding: 0; box-sizing: border-box; }}

    body {{
      background-color: #f8fafc;
      font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      color: #1f2937;
      -webkit-font-smoothing: antialiased;
    }}

    .email-wrapper {{
      max-width: 620px;
      margin: 40px auto;
      background: #ffffff;
      border-radius: 16px;
      overflow: hidden;
      box-shadow: 0 4px 24px rgba(0,0,0,0.08);
    }}

    /* ── Header ── */
    .header {{
      background: linear-gradient(135deg, #f08d11 0%, #ff9f2e 100%);
      padding: 36px 48px 32px;
      text-align: center;
    }}

    .header-logo-wrap {{
      display: inline-block;
      background: #ffffff;
      border-radius: 14px;
      padding: 10px 22px;
      margin-bottom: 22px;
      box-shadow: 0 2px 12px rgba(0,0,0,0.12);
    }}

    .header-logo-wrap img {{
      height: 44px;
      width: auto;
      display: block;
    }}

    .header h1 {{
      font-size: 27px;
      font-weight: 800;
      color: #ffffff;
      line-height: 1.25;
      margin-bottom: 8px;
    }}

    .header .activation-badge {{
      display: inline-block;
      background: rgba(255,255,255,0.22);
      border: 1px solid rgba(255,255,255,0.40);
      border-radius: 20px;
      padding: 5px 16px;
      font-size: 13.5px;
      font-weight: 600;
      color: #ffffff;
      margin-top: 6px;
      letter-spacing: 0.2px;
    }}

    /* ── Body ── */
    .body {{
      padding: 40px 48px;
    }}

    .greeting {{
      font-size: 17px;
      font-weight: 600;
      color: #111827;
      margin-bottom: 14px;
    }}

    .intro {{
      font-size: 14.5px;
      color: #4b5563;
      line-height: 1.75;
      margin-bottom: 32px;
    }}

    /* ── Section title ── */
    .section-title {{
      font-size: 16px;
      font-weight: 700;
      color: #111827;
      margin-bottom: 18px;
      padding-bottom: 10px;
      border-bottom: 2px solid #f08d11;
    }}

    /* ── Step card ── */
    .step-card {{
      background: #f9fafb;
      border-left: 4px solid #f08d11;
      border-radius: 8px;
      padding: 18px 20px;
      margin-bottom: 14px;
    }}

    .step-label {{
      font-size: 13px;
      font-weight: 700;
      color: #f08d11;
      text-transform: uppercase;
      letter-spacing: 0.6px;
      margin-bottom: 6px;
    }}

    .step-card h3 {{
      font-size: 15px;
      font-weight: 700;
      color: #111827;
      margin-bottom: 10px;
    }}

    .step-card ul {{
      list-style: none;
      padding: 0;
    }}

    .step-card ul li {{
      font-size: 14px;
      color: #374151;
      line-height: 1.7;
      padding-left: 18px;
      position: relative;
      margin-bottom: 5px;
    }}

    .step-card ul li::before {{
      content: '›';
      position: absolute;
      left: 0;
      color: #f08d11;
      font-weight: 700;
    }}

    .step-card ul li strong {{
      color: #111827;
      font-weight: 600;
    }}

    /* ── Divider ── */
    .divider {{
      border: none;
      border-top: 1px solid #e5e7eb;
      margin: 28px 0;
    }}

    /* ── Summary checklist ── */
    .checklist {{
      list-style: none;
      padding: 0;
      margin-bottom: 28px;
    }}

    .checklist li {{
      display: flex;
      align-items: flex-start;
      gap: 12px;
      margin-bottom: 14px;
    }}

    .check-icon {{
      width: 10px;
    height: 10px;
    min-width: 10px;
    background: linear-gradient(135deg, #ffb347, #f08d11);
    border-radius: 50%;
    display: inline-block;
    margin-top: 6px;
    margin-right: 10px;
    box-shadow: 0 2px 6px rgba(240, 141, 17, 0.35);
    }}

    .check-icon svg {{
      width: 12px;
      height: 12px;
    }}

    .checklist li .check-text {{
      font-size: 14px;
      color: #374151;
      line-height: 1.6;
    }}

    .checklist li .check-text strong {{
      color: #111827;
      font-weight: 600;
    }}

    /* ── CTA button ── */
    .cta-wrapper {{
      text-align: center;
      margin: 32px 0 10px;
    }}

    .cta-button {{
      display: inline-block;
      background: linear-gradient(135deg, #f08d11 0%, #ff9f2e 100%);
      color: #ffffff !important;
      text-decoration: none;
      font-size: 15px;
      font-weight: 700;
      padding: 14px 36px;
      border-radius: 8px;
      letter-spacing: 0.2px;
    }}

    /* ── Support note ── */
    .support-note {{
      background: #fff7ed;
      border: 1px solid #fed7aa;
      border-radius: 8px;
      padding: 16px 18px;
      margin-top: 24px;
      font-size: 13.5px;
      color: #92400e;
      line-height: 1.65;
    }}

    .support-note a {{
      color: #f08d11;
      font-weight: 600;
      text-decoration: none;
    }}

    /* ── Footer ── */
    .footer {{
      background: #f3f4f6;
      padding: 24px 48px;
      text-align: center;
    }}

    .footer p {{
      font-size: 12.5px;
      color: #9ca3af;
      line-height: 1.7;
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

    <!-- Header -->
    <div class=""header"">
      <div class=""header-logo-wrap"">
        <img src=""{logoUrl}"" alt=""Menu Optics"" />
      </div>
      <h1>Welcome to Menu Optics! 🚀</h1>
    </div>

    <!-- Body -->
    <div class=""body"">
      <p class=""intro"">
        Starting {currentDate}, your <strong>{restaurant}</strong> account will officially have <strong>Menu Optics</strong> enabled.
        We are thrilled to bring you a more streamlined, data-driven approach to managing
        your operational inventory and product pricing.<br /><br />
        Here is everything you need to know about getting started, what to expect over your
        first few days, and how to get the most out of the platform.
      </p>

      <!-- Step-by-Step Onboarding -->
      <p class=""section-title"">📋 Step-by-Step Onboarding Guide</p>

      <!-- Step 1 -->
      <div class=""step-card"">
        <p class=""step-label"">Step 1</p>
        <h3>Account Activation &amp; First-Time Login</h3>
        <ul>
          <li>Go to <strong><a href=""https://www.menuoptics.com"" style=""color:#f08d11;text-decoration:none;"">www.menuoptics.com</a></strong></li>
          <li>Enter the <strong>email address</strong> associated with your account.</li>
          <li>Check your inbox for a <strong>One-Time Passcode (OTP)</strong> sent by Menu Optics.</li>
          <li>Enter the OTP on the login page and follow the prompt to <strong>create your new password</strong>.</li>
        </ul>
      </div>

      <!-- Step 2 -->
      <div class=""step-card"">
        <p class=""step-label"">Step 2</p>
        <h3>Product &amp; Inventory Catalog Sync</h3>
        <ul>
          <li>
            <strong>Product Catalog:</strong> Within <strong>4 hours</strong> of completing your registration,
            your full <strong>Flanagan Product Catalog</strong> will automatically load into your Menu Optics profile.
          </li>
          <li>
            <strong>Invoice Data:</strong> Any new invoices generated from your activation date onward will begin
            populating automatically in the <strong>Inventory Suite</strong> within <strong>1 business day following delivery</strong>.
          </li>
        </ul>
      </div>

      <hr class=""divider"" />

      <!-- Training & Support -->
      <p class=""section-title"">🎓 Training &amp; Support</p>
      <div class=""step-card"">
        <ul>
          <li>
            <strong>Self-Guided Videos:</strong> Short instructional videos are embedded directly on each
            page of the platform to guide you through key features and workflows.
          </li>
          <li>
            <strong>Dedicated Support:</strong> If you run into any issues or have quick questions during setup,
            our team is standing by to help at
            <a href=""mailto:support@menuoptics.com"" style=""color:#f08d11;font-weight:600;text-decoration:none;"">support@menuoptics.com</a>.
          </li>
        </ul>
      </div>

      <hr class=""divider"" />

      <!-- Next Steps Summary -->
      <p class=""section-title"">✅ Your Next Steps Summary</p>
      <ul class=""checklist"">
        <li>
          <span class=""check-icon"">
            <svg viewBox=""0 0 12 12"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
              <path d=""M2 6l3 3 5-5"" stroke=""#fff"" stroke-width=""1.8"" stroke-linecap=""round"" stroke-linejoin=""round""/>
            </svg>
          </span>
          <span class=""check-text"">
            <strong>Log in &amp; set your password:</strong> Visit
            <a href=""https://www.menuoptics.com"" style=""color:#f08d11;font-weight:600;text-decoration:none;"">www.menuoptics.com</a>
            to log in and set your permanent password.
          </span>
        </li>
        <li>
          <span class=""check-icon"">
            <svg viewBox=""0 0 12 12"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
              <path d=""M2 6l3 3 5-5"" stroke=""#fff"" stroke-width=""1.8"" stroke-linecap=""round"" stroke-linejoin=""round""/>
            </svg>
          </span>
          <span class=""check-text"">
            <strong>Explore the System:</strong> Watch the embedded page guides and start getting familiar
            with your active dashboard and product data.
          </span>
        </li>
        <li>
          <span class=""check-icon"">
            <svg viewBox=""0 0 12 12"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
              <path d=""M2 6l3 3 5-5"" stroke=""#fff"" stroke-width=""1.8"" stroke-linecap=""round"" stroke-linejoin=""round""/>
            </svg>
          </span>
          <span class=""check-text"">
            <strong>Share Your Feedback:</strong> After using the system for <strong>two weeks</strong>,
            you will receive a brief user experience survey. We want your direct input on functionality,
            layout, and potential enhancements so we can keep building tools that best serve your business!
          </span>
        </li>
      </ul>

      <!-- CTA -->
      <div class=""cta-wrapper"">
        <a href=""{baseUrl}/User/Login?email={email}"" class=""cta-button"">Get Started →</a>
      </div>

      <!-- Support note -->
      <div class=""support-note"">
        Need help? Reach out anytime at
        <a href=""mailto:support@menuoptics.com"" target=""_blank"">support@menuoptics.com</a> —
        our team is here to make your onboarding as smooth as possible.
      </div>
    </div>

    <!-- Footer -->
    <div class=""footer"">
      <p>
        © {DateTime.Now.Year} Menu Optics. All rights reserved.<br />
        <a href=""https://www.menuoptics.com"">www.menuoptics.com</a>
      </p>
    </div>

  </div>
</body>
</html>";
        }
    }
}