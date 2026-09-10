using System.Net;
using MenuOptics.Models;
using MenuOptics.Models.Helper;

namespace MenuOptics.Services.EmailTemplates
{
    public static class ContactEmailTemplate
    {
        public static string BuildAdminNotification(ContactInquiry inquiry, string baseUrl = "")
        {
            var fullName = $"{WebUtility.HtmlEncode(inquiry.FirstName)} {WebUtility.HtmlEncode(inquiry.LastName)}".Trim();
            var email = WebUtility.HtmlEncode(inquiry.Email ?? string.Empty);
            var phone = string.IsNullOrWhiteSpace(inquiry.PhoneNumber) ? "Not provided" : WebUtility.HtmlEncode(inquiry.PhoneNumber);
            var reason = WebUtility.HtmlEncode(inquiry.Reason ?? "General Inquiry");
            var preferredMethod = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(inquiry.PreferredContactMethod) ? "Email" : inquiry.PreferredContactMethod);
            var messageContent = WebUtility.HtmlEncode(inquiry.Message ?? string.Empty).Replace("\n", "<br />");

            var today = DateTime.UtcNow;
            var formattedDate = today.ToString("dddd, MMMM d, yyyy 'at' h:mm tt 'UTC'");

            var logoUrl = string.IsNullOrWhiteSpace(baseUrl)
                ? "https://menuoptics.com/images/menu%20optics_logo.png"
                : $"{baseUrl.TrimEnd('/')}/images/menu%20optics_logo.png";

            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>New Contact Inquiry - Menu Optics</title>
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
      margin-top: 4px;
    }}
    .body {{
      padding: 32px 36px;
    }}
    .lead-text {{
      font-size: 15px;
      color: #374151;
      margin-bottom: 22px;
      line-height: 1.6;
    }}
    .details-table {{
      width: 100%;
      border-collapse: separate;
      border-spacing: 0;
      background: #f9fafb;
      border: 1px solid #e5e7eb;
      border-radius: 12px;
      margin-bottom: 24px;
      overflow: hidden;
    }}
    .details-table td {{
      padding: 12px 16px;
      font-size: 14px;
      border-bottom: 1px solid #edf2f7;
    }}
    .details-table tr:last-child td {{
      border-bottom: none;
    }}
    .field-label {{
      color: #6b7280;
      font-weight: 600;
      width: 38%;
    }}
    .field-value {{
      color: #111827;
      font-weight: 500;
    }}
    .topic-pill {{
      display: inline-block;
      background: #fff7ed;
      color: #c2410c;
      border: 1px solid #fed7aa;
      font-weight: 700;
      font-size: 12.5px;
      padding: 3px 10px;
      border-radius: 6px;
    }}
    .message-section-title {{
      font-size: 14px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: #4b5563;
      margin-bottom: 8px;
    }}
    .message-box {{
      background: #ffffff;
      border-left: 4px solid #f08d11;
      border-top: 1px solid #e5e7eb;
      border-right: 1px solid #e5e7eb;
      border-bottom: 1px solid #e5e7eb;
      border-radius: 0 10px 10px 0;
      padding: 18px 20px;
      font-size: 14.5px;
      line-height: 1.7;
      color: #1f2937;
      margin-bottom: 28px;
      word-break: break-word;
    }}
    .action-wrap {{
      text-align: center;
      margin: 28px 0 16px;
    }}
    .action-btn {{
      display: inline-block;
      background: #f08d11;
      color: #ffffff !important;
      text-decoration: none;
      font-weight: 700;
      font-size: 14.5px;
      padding: 13px 32px;
      border-radius: 8px;
      box-shadow: 0 4px 14px rgba(240,141,17,0.35);
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
      <h1>New Contact Inquiry</h1>
      <span class=""badge"">{reason}</span>
    </div>

    <div class=""body"">
      <p class=""lead-text"">
        You have received a new inquiry from the <strong>Menu Optics Contact Us</strong> form.
      </p>

      <table class=""details-table"">
        <tr>
          <td class=""field-label"">Inquirer Name</td>
          <td class=""field-value""><strong>{fullName}</strong></td>
        </tr>
        <tr>
          <td class=""field-label"">Email Address</td>
          <td class=""field-value"">
            <a href=""mailto:{email}"" style=""color:#f08d11;text-decoration:none;font-weight:600;"">{email}</a>
          </td>
        </tr>
        <tr>
          <td class=""field-label"">Phone Number</td>
          <td class=""field-value"">{phone}</td>
        </tr>
        <tr>
          <td class=""field-label"">Reason for Inquiry</td>
          <td class=""field-value""><span class=""topic-pill"">{reason}</span></td>
        </tr>
        <tr>
          <td class=""field-label"">Preferred Contact</td>
          <td class=""field-value"">{preferredMethod}</td>
        </tr>
        <tr>
          <td class=""field-label"">Submitted At</td>
          <td class=""field-value"">{formattedDate}</td>
        </tr>
      </table>

      <div class=""message-section-title"">Inquiry Message</div>
      <div class=""message-box"">
        {messageContent}
      </div>

      <div class=""action-wrap"">
        <a href=""mailto:{email}?subject=Re: {reason} - Menu Optics"" class=""action-btn"">
          Reply to {fullName} &rarr;
        </a>
      </div>
    </div>

    <div class=""footer"">
      <p>
        Menu Optics Operations &bull; Real-Time Food Costing &amp; AI Menu Engineering<br />
        <a href=""https://www.menuoptics.com"">www.menuoptics.com</a>
      </p>
    </div>
  </div>
</body>
</html>";
        }

        public static string BuildUserAcknowledgment(ContactInquiry inquiry, string baseUrl = "")
        {
            var firstName = WebUtility.HtmlEncode(inquiry.FirstName ?? "there");
            var reason = WebUtility.HtmlEncode(inquiry.Reason ?? "General Inquiry");
            var messageContent = WebUtility.HtmlEncode(inquiry.Message ?? string.Empty).Replace("\n", "<br />");

            var logoUrl = string.IsNullOrWhiteSpace(baseUrl)
                ? "https://menuoptics.com/images/menu%20optics_logo.png"
                : $"{baseUrl.TrimEnd('/')}/images/menu%20optics_logo.png";

            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>We Received Your Message - Menu Optics</title>
  <style>
    @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap');
    * {{ margin: 0; padding: 0; box-sizing: border-box; }}
    body {{
      background-color: #f8fafc;
      font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      color: #1f2937;
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
      font-size: 18px;
      font-weight: 700;
      color: #111827;
      margin-bottom: 12px;
    }}
    .intro {{
      font-size: 14.5px;
      color: #4b5563;
      line-height: 1.7;
      margin-bottom: 24px;
    }}
    .summary-card {{
      background: #f9fafb;
      border: 1px solid #e5e7eb;
      border-left: 4px solid #f08d11;
      border-radius: 8px;
      padding: 16px 20px;
      margin-bottom: 24px;
      font-size: 14px;
      color: #374151;
      line-height: 1.6;
    }}
    .summary-card strong {{
      color: #111827;
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
      <h1>We Received Your Message</h1>
      <span class=""badge"">24-Hour Response Guarantee</span>
    </div>

    <div class=""body"">
      <div class=""greeting"">Hi {firstName},</div>
      <p class=""intro"">
        Thank you for reaching out to the Menu Optics team! We have received your inquiry regarding <strong>{reason}</strong> and our team has been notified. We will review your request and get back to you within 24 hours.
      </p>

      <div class=""summary-card"">
        <p style=""margin-bottom:6px;""><strong>Your Submitted Message:</strong></p>
        <p style=""color:#4b5563;font-style:italic;"">{messageContent}</p>
      </div>

      <p class=""intro"" style=""margin-bottom:0;"">
        If you have urgent questions, you can also explore our self-guided guides and FAQs on our platform at any time.
      </p>
    </div>

    <div class=""footer"">
      <p>
        &copy; {DateTime.Now.Year} Menu Optics. All rights reserved.<br />
        <a href=""https://www.menuoptics.com"">www.menuoptics.com</a>
      </p>
    </div>
  </div>
</body>
</html>";
        }
    }
}
