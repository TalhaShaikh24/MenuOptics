using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Data;
using Restaurant.Models;
using Stripe;
using Stripe.Checkout;
using System.Data;
using System.Security.Claims;

namespace Restaurant.Controllers
{
    public class PaymentController : Controller
    {

        private readonly DapperContext _context;
        public PaymentController(DapperContext context)
        {
            _context = context;
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
            var response = new Response();
            try
            {
                // Configure Stripe API key
                StripeConfiguration.ApiKey = "sk_test_51M9O4qKR3yBF1l8fZJ5Ll6Q7x0V0ec9i8rgqi8FMKtHTqSx3WYMvdCeOpuOdpImJ6OmhTiq6DDUhB83zumFikT2700QEmhSeSP";

                // Validate session_id
                if (string.IsNullOrEmpty(session_id))
                {
                    response.Status = 400;
                    response.ResponseMsg = "Session ID is missing.";
                    return BadRequest(response);
                }

                // Retrieve the Stripe session
                var sessionService = new SessionService();
                var session = await sessionService.GetAsync(session_id);

                // Retrieve the subscription details
                var subscriptionService = new SubscriptionService();
                var subscription = await subscriptionService.GetAsync(session.SubscriptionId);


                if (string.IsNullOrEmpty(user_id))
                {
                    response.Status = 400;
                    response.ResponseMsg = "User ID not found in session metadata.";
                    return BadRequest(response);
                }

                // Update the user in the database
                using var connection = _context.CreateConnection();
                connection.Open();
                try
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@UserId", Convert.ToInt32(user_id));
                    parameters.Add("@StripeCustomerId", session.CustomerId);
                    parameters.Add("@StripeSubscriptionId", session.SubscriptionId);
                    parameters.Add("@SubscriptionStatus", subscription.Status == "active" || subscription.Status == "trialing" ? "Active" : "Pending");
                    parameters.Add("@Action", "UPDATE_SUBSCRIPTION");

                    await connection.ExecuteAsync("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                    response.Status = 200;
                    response.ResponseMsg = "Subscription updated successfully.";
                    response.Data = new { UserId = Convert.ToInt32(user_id), SubscriptionStatus = subscription.Status };
                    return Ok(response);
                }
                finally
                {
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
        }
    }
}
