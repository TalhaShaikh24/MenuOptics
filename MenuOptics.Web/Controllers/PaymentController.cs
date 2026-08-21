using Microsoft.AspNetCore.Mvc;

namespace MenuOptics.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;

        public PaymentController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:7104/";
            if (!_apiBaseUrl.EndsWith("/"))
            {
                _apiBaseUrl += "/";
            }
        }

        public IActionResult Success()
        {
            return View();
        }

        [HttpGet("/Payment/successpayment")]
        public async Task<IActionResult> SuccessPayment([FromQuery] string? session_id, [FromQuery] string? user_id)
        {
            try
            {
                if (!string.IsNullOrEmpty(session_id))
                {
                    var client = _httpClientFactory.CreateClient("ApiClient");
                    var apiUrl = $"{_apiBaseUrl}api/Payment/successpayment?session_id={session_id}&user_id={user_id}";
                    await client.GetAsync(apiUrl);
                }
            }
            catch
            {
                // Proceed to UI redirect even if background sync encounters a network issue
            }

            // If user is logged in (e.g. from Dashboard Renew / Reactivate), redirect to Subscription screen
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Subscription");
            }

            // If user just registered, show the Success confirmation view
            return View("Success");
        }

        public IActionResult Cancel()
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Subscription");
            }
            return View();
        }
    }
}
