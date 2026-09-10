using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Models;
using MenuOptics.Models.Helper;
using MenuOptics.Services;
using MenuOptics.Services.EmailTemplates;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class ContactApiController : ControllerBase
    {
        private readonly ISmtpEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ContactApiController> _logger;

        public ContactApiController(
            ISmtpEmailService emailService,
            IConfiguration configuration,
            ILogger<ContactApiController> logger)
        {
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("SendInquiry")]
        public async Task<IActionResult> SendInquiry([FromBody] ContactInquiry model)
        {
            if (model == null)
            {
                return BadRequest(new ResponseDto
                {
                    Status = 400,
                    ResponseMsg = "Invalid inquiry data provided."
                });
            }

            if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
            {
                return BadRequest(new ResponseDto
                {
                    Status = 400,
                    ResponseMsg = "First name and last name are required."
                });
            }

            if (string.IsNullOrWhiteSpace(model.Email) || !model.Email.Contains("@"))
            {
                return BadRequest(new ResponseDto
                {
                    Status = 400,
                    ResponseMsg = "Please provide a valid email address."
                });
            }

            if (string.IsNullOrWhiteSpace(model.Reason))
            {
                return BadRequest(new ResponseDto
                {
                    Status = 400,
                    ResponseMsg = "Please select a reason for your inquiry."
                });
            }

            if (string.IsNullOrWhiteSpace(model.Message))
            {
                return BadRequest(new ResponseDto
                {
                    Status = 400,
                    ResponseMsg = "Message cannot be empty."
                });
            }

            if (model.Message.Length > 1000)
            {
                return BadRequest(new ResponseDto
                {
                    Status = 400,
                    ResponseMsg = "Message cannot exceed 1,000 characters."
                });
            }

            try
            {
                var webUrl = _configuration["WebUrl"] ?? "https://menuoptics.com";
                var adminRecipient =  _configuration["Email:SupportEmail"];

                // 1. Build and send inquiry notification to support/admin
                var adminHtml = ContactEmailTemplate.BuildAdminNotification(model, webUrl);
                var adminSubject = $"[Menu Optics Inquiry] {model.Reason} - {model.FirstName} {model.LastName}";

                await _emailService.SendEmailAsync(adminRecipient, adminSubject, adminHtml);

                // 2. Send polite acknowledgment to the inquirer
                try
                {
                    var userHtml = ContactEmailTemplate.BuildUserAcknowledgment(model, webUrl);
                    var userSubject = "We received your message - Menu Optics Support";
                    await _emailService.SendEmailAsync(model.Email, userSubject, userHtml);
                }
                catch (Exception ackEx)
                {
                    _logger.LogWarning(ackEx, "Could not send auto-acknowledgment email to inquirer {Email}", model.Email);
                }

                return Ok(new ResponseDto
                {
                    Status = 200,
                    ResponseMsg = "Thank you! Your message has been sent successfully. Our team will respond within 24 hours."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send contact inquiry from {Email}", model?.Email);
                return StatusCode(500, new ResponseDto
                {
                    Status = 500,
                    ResponseMsg = "An error occurred while processing your message. Please try again or reach out to support@menuoptics.com."
                });
            }
        }
    }
}
