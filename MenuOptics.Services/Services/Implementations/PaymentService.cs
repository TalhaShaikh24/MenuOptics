using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using Stripe;
using Stripe.Checkout;
using System.Data;

namespace MenuOptics.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly DapperContext _context;

        public PaymentService(DapperContext context)
        {
            _context = context;
        }

        public async Task<ResponseDto> PaymentSuccessAsync(string? sessionId, string? userId)
        {
            var ResponseDto = new ResponseDto();
            try
            {
                // Configure Stripe API key
                StripeConfiguration.ApiKey = "sk_test_51M9O4qKR3yBF1l8fZJ5Ll6Q7x0V0ec9i8rgqi8FMKtHTqSx3WYMvdCeOpuOdpImJ6OmhTiq6DDUhB83zumFikT2700QEmhSeSP";

                // Validate session_id
                if (string.IsNullOrEmpty(sessionId))
                {
                    ResponseDto.Status = 400;
                    ResponseDto.ResponseMsg = "Session ID is missing.";
                    return ResponseDto;
                }

                // Retrieve the Stripe session
                var sessionService = new SessionService();
                var session = await sessionService.GetAsync(sessionId);

                // Retrieve the subscription details
                var subscriptionService = new SubscriptionService();
                var subscription = await subscriptionService.GetAsync(session.SubscriptionId);

                if (string.IsNullOrEmpty(userId))
                {
                    ResponseDto.Status = 400;
                    ResponseDto.ResponseMsg = "User ID not found in session metadata.";
                    return ResponseDto;
                }

                // Update the user in the database
                using var connection = _context.CreateConnection();
                connection.Open();
                try
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@UserId", Convert.ToInt32(userId));
                    parameters.Add("@StripeCustomerId", session.CustomerId);
                    parameters.Add("@StripeSubscriptionId", session.SubscriptionId);
                    parameters.Add("@SubscriptionStatus", subscription.Status == "active" || subscription.Status == "trialing" ? "Active" : "Pending");
                    parameters.Add("@Action", "UPDATE_SUBSCRIPTION");

                    await connection.ExecuteAsync("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                    ResponseDto.Status = 200;
                    ResponseDto.ResponseMsg = "Subscription updated successfully.";
                    ResponseDto.Data = new { UserId = Convert.ToInt32(userId), SubscriptionStatus = subscription.Status };
                    return ResponseDto;
                }
                finally
                {
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";
                return ResponseDto;
            }
        }
    }
}
