using MenuOptics.Models;
using MenuOptics.Models.DTOs.Subscription;
using MenuOptics.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class SubscriptionApiController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionApiController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        private int EffectiveUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        private int ActualUserId => int.Parse(User.FindFirst("ActualUserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        private bool IsParent => bool.TryParse(User.FindFirst("IsParent")?.Value, out var isParent) ? isParent : true;
        private bool IsVendorCreated => bool.TryParse(User.FindFirst("IsVendorCreated")?.Value, out var isVendorCreated) && isVendorCreated;

        private string RequestBaseUrl
        {
            get
            {
                var origin = Request.Headers["Origin"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(origin))
                {
                    return origin.TrimEnd('/');
                }

                var referer = Request.Headers["Referer"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var uri))
                {
                    return $"{uri.Scheme}://{uri.Authority}".TrimEnd('/');
                }

                return $"{Request.Scheme}://{Request.Host}".TrimEnd('/');
            }
        }

        [HttpGet("GetDetails")]
        public async Task<IActionResult> GetDetails()
        {
            if (IsVendorCreated)
            {
                return Ok(new ResponseDto
                {
                    Status = 200,
                    ResponseMsg = "Vendor partner restaurant - complimentary access.",
                    Data = new SubscriptionDetailsDto
                    {
                        SubscriptionStatus = "Active",
                        Plan = "Vendor Partner (Complimentary)",
                        IsActiveSubscription = true
                    }
                });
            }

            var response = await _subscriptionService.GetSubscriptionDetailsAsync(EffectiveUserId, ActualUserId, IsParent);
            return StatusCode(response.Status, response);
        }

        [HttpPost("Cancel")]
        public async Task<IActionResult> Cancel()
        {
            if (IsVendorCreated)
            {
                return StatusCode(400, new ResponseDto
                {
                    Status = 400,
                    ResponseMsg = "Vendor managed restaurants have complimentary access."
                });
            }

            var response = await _subscriptionService.CancelSubscriptionAsync(EffectiveUserId, ActualUserId, IsParent);
            return StatusCode(response.Status, response);
        }

        [HttpPost("Renew")]
        public async Task<IActionResult> Renew([FromBody] RenewSubscriptionRequestDto dto)
        {
            if (IsVendorCreated)
            {
                return StatusCode(400, new ResponseDto
                {
                    Status = 400,
                    ResponseMsg = "Vendor managed restaurants have complimentary access."
                });
            }

            if (!IsParent)
            {
                return StatusCode(403, new ResponseDto
                {
                    Status = 403,
                    ResponseMsg = "Only the restaurant owner can manage or renew subscriptions."
                });
            }

            var plan = string.IsNullOrWhiteSpace(dto?.Plan) ? "professional" : dto.Plan;
            var response = await _subscriptionService.CreateCheckoutSessionAsync(EffectiveUserId, plan, RequestBaseUrl);
            return StatusCode(response.Status, response);
        }

        [HttpPost("RetryPayment")]
        public async Task<IActionResult> RetryPayment()
        {
            if (IsVendorCreated)
            {
                return StatusCode(400, new ResponseDto
                {
                    Status = 400,
                    ResponseMsg = "Vendor managed restaurants have complimentary access."
                });
            }

            var response = await _subscriptionService.RetryPaymentAsync(EffectiveUserId, ActualUserId, IsParent, RequestBaseUrl);
            return StatusCode(response.Status, response);
        }
    }
}
