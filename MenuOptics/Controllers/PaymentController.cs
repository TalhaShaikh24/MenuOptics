using Microsoft.AspNetCore.Mvc;
using MenuOptics.Services;

namespace MenuOptics.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        public IActionResult Success()
        {
            return View();
        }

        public IActionResult Cancel()
        {
            return View();
        }

        [HttpGet("/payment/successpayment")]
        public async Task<IActionResult> PaymentSuccess([FromQuery] string? session_id, string? user_id)
        {
            var response = await _paymentService.PaymentSuccessAsync(session_id, user_id);
            return StatusCode(response.Status, response);
        }
    }
}
