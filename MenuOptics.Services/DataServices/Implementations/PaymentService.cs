using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using MenuOptics.Models.Enums;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly DapperContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PaymentService(DapperContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public (string SessionId, string? Url) CreateCheckoutSession(string? plan, int userId)
        {
            var stripeKey = _configuration["Stripe:SecretKey"];
            StripeConfiguration.ApiKey = stripeKey;

            var starterPrice = _configuration["Stripe:StarterPriceId"];
            var proPrice = _configuration["Stripe:ProfessionalPriceId"];

            var priceId = (plan?.ToLower()) switch
            {
                "starter" => starterPrice,
                _ => proPrice
            };

            // Dynamically resolve client Web Base URL from Origin or Referer header
            var httpContext = _httpContextAccessor.HttpContext;
            string? origin = httpContext?.Request.Headers["Origin"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(origin))
            {
                var referer = httpContext?.Request.Headers["Referer"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var uri))
                {
                    origin = $"{uri.Scheme}://{uri.Authority}";
                }
            }

            var baseUrl = !string.IsNullOrWhiteSpace(origin)
                ? origin.TrimEnd('/')
                : (_configuration["WebUrl"] ?? "https://localhost:7103").TrimEnd('/');

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "subscription",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1,
                    },
                },
                Metadata = new Dictionary<string, string>
                {
                    { "UserId", userId.ToString() }
                },
                AllowPromotionCodes = true,
                SuccessUrl = $"{baseUrl}/Payment/successpayment?session_id={{CHECKOUT_SESSION_ID}}&user_id={userId}",
                CancelUrl = $"{baseUrl}/Payment/Cancel",
            };

            var service = new SessionService();
            var session = service.Create(options);

            return (session.Id, session.Url);
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
            catch (SqlException sqlEx)
            {
                responseDto.Status = 400;
                responseDto.ResponseMsg = sqlEx.Message;
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = $"{ex.Message}";
                return responseDto;
            }
        }
    }
}
