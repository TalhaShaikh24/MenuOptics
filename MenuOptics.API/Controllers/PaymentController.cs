using Microsoft.AspNetCore.Mvc;
using MenuOptics.Services;

namespace MenuOptics.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet("successpayment")]
        public async Task<IActionResult> PaymentSuccess([FromQuery] string? session_id, string? user_id)
        {
            var responseData = await _paymentService.PaymentSuccessAsync(session_id, user_id);
            return StatusCode(responseData.Status, responseData);
        }
    }
}
