using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using MenuOptics.Models.Enums;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using System.Data;

namespace MenuOptics.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly DapperContext _context;
        private readonly IConfiguration _configuration;

        public PaymentService(DapperContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<ResponseDto> PaymentSuccessAsync(string? sessionId, string? userId)
        {
            var responseDto = new ResponseDto();
            try
            {
                // Configure Stripe API key from configuration
                var stripeKey = _configuration["Stripe:SecretKey"];
                StripeConfiguration.ApiKey = stripeKey;

                // Validate session_id
                if (string.IsNullOrEmpty(sessionId))
                {
                    responseDto.Status = 400;
                    responseDto.ResponseMsg = "Session ID is missing.";
                    return responseDto;
                }

                // Retrieve the Stripe session
                var sessionService = new SessionService();
                var session = await sessionService.GetAsync(sessionId);

                // Retrieve subscription details if subscription ID is present
                string status = PaymentStatus.Active.ToString();
                if (!string.IsNullOrEmpty(session.SubscriptionId))
                {
                    var subscriptionService = new Stripe.SubscriptionService();
                    var subscription = await subscriptionService.GetAsync(session.SubscriptionId);
                    status = (subscription.Status == "active" || subscription.Status == "trialing")
                        ? PaymentStatus.Active.ToString()
                        : PaymentStatus.Pending.ToString();
                }

                // If userId not provided in query param, check session metadata
                if (string.IsNullOrEmpty(userId) && session.Metadata != null && session.Metadata.TryGetValue("UserId", out var metaUserId))
                {
                    userId = metaUserId;
                }

                if (string.IsNullOrEmpty(userId))
                {
                    responseDto.Status = 400;
                    responseDto.ResponseMsg = "User ID not found in request or session metadata.";
                    return responseDto;
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
                    parameters.Add("@SubscriptionStatus", status);
                    parameters.Add("@Action", "UPDATE_SUBSCRIPTION");

                    await connection.ExecuteAsync("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                    responseDto.Status = 200;
                    responseDto.ResponseMsg = "Subscription updated successfully.";
                    responseDto.Data = new { UserId = Convert.ToInt32(userId), SubscriptionStatus = status };
                    return responseDto;
                }
                finally
                {
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = $"Server error: {ex.Message}";
                return responseDto;
            }
        }
    }
}
