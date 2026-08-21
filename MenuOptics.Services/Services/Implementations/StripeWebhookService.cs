using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using MenuOptics.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using System.Data;

namespace MenuOptics.Services
{
    public class StripeWebhookService : IStripeWebhookService
    {
        private readonly DapperContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripeWebhookService> _logger;

        public StripeWebhookService(
            DapperContext context,
            IConfiguration configuration,
            ILogger<StripeWebhookService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(int StatusCode, object? Body)> HandleWebhookAsync(string json, string? stripeSignature)
        {
            try
            {
                var webhookSecret = _configuration["Stripe:WebhookSecret"]
                    ?? "whsec_5faaa8f893e8ee04fe332d778da9a4b4807614c9f57791447440cfdcb58bca33";

                Stripe.Event stripeEvent;
                if (!string.IsNullOrEmpty(stripeSignature))
                {
                    stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
                }
                else
                {
                    // Fallback parse if signature header is absent (e.g. testing)
                    stripeEvent = EventUtility.ParseEvent(json);
                }

                _logger.LogInformation("Processing Stripe Webhook Event: {EventType}", stripeEvent.Type);

                using var connection = _context.CreateConnection();
                connection.Open();

                try
                {
                    switch (stripeEvent.Type)
                    {
                        case Events.CustomerSubscriptionCreated:
                        case Events.CustomerSubscriptionUpdated:
                        case Events.CustomerSubscriptionDeleted:
                        {
                            if (stripeEvent.Data.Object is Subscription subscription)
                            {
                                await HandleSubscriptionEventAsync(connection, subscription);
                            }
                            break;
                        }

                        case Events.CheckoutSessionCompleted:
                        {
                            if (stripeEvent.Data.Object is Session session)
                            {
                                await HandleCheckoutSessionCompletedAsync(connection, session);
                            }
                            break;
                        }

                        case Events.InvoicePaymentSucceeded:
                        {
                            if (stripeEvent.Data.Object is Invoice invoice)
                            {
                                await HandleInvoicePaymentSucceededAsync(connection, invoice);
                            }
                            break;
                        }

                        case Events.InvoicePaymentFailed:
                        {
                            if (stripeEvent.Data.Object is Invoice invoice)
                            {
                                await HandleInvoicePaymentFailedAsync(connection, invoice);
                            }
                            break;
                        }

                        default:
                            _logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                            break;
                    }
                }
                finally
                {
                    connection.Close();
                }

                return (200, new { received = true });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe Webhook Signature Verification Failed: {Message}", ex.Message);
                return (400, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe Webhook Processing Error: {Message}", ex.Message);
                return (500, new { error = ex.Message });
            }
        }

        private async Task HandleSubscriptionEventAsync(IDbConnection connection, Subscription subscription)
        {
            var status = MapStripeSubscriptionStatus(subscription.Status);

            // Find user by subscription ID, customer ID, or metadata
            int? userId = null;
            if (subscription.Metadata != null && subscription.Metadata.TryGetValue("UserId", out var userIdStr) && int.TryParse(userIdStr, out var parsedId))
            {
                userId = parsedId;
            }

            if (!userId.HasValue)
            {
                var findParams = new DynamicParameters();
                findParams.Add("@Action", "SELECT_ALL");
                var allUsers = await connection.QueryAsync<UserDto>("sp_UserCRUD", findParams, commandType: CommandType.StoredProcedure);
                var matchedUser = allUsers.FirstOrDefault(u =>
                    u.StripeSubscriptionId == subscription.Id ||
                    (!string.IsNullOrEmpty(u.StripeCustomerId) && u.StripeCustomerId == subscription.CustomerId));

                if (matchedUser != null)
                {
                    userId = matchedUser.UserId;
                }
            }

            if (userId.HasValue)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", userId.Value);
                parameters.Add("@StripeCustomerId", subscription.CustomerId);
                parameters.Add("@StripeSubscriptionId", subscription.Id);
                parameters.Add("@SubscriptionStatus", status);
                parameters.Add("@Action", "UPDATE_SUBSCRIPTION");
                await connection.ExecuteAsync("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                _logger.LogInformation("Updated User {UserId} subscription status to {Status} from Subscription event", userId.Value, status);
            }
            else
            {
                _logger.LogWarning("No user found matching Stripe Subscription ID {SubscriptionId} or Customer ID {CustomerId}", subscription.Id, subscription.CustomerId);
            }
        }

        private async Task HandleCheckoutSessionCompletedAsync(IDbConnection connection, Session session)
        {
            int? userId = null;
            if (session.Metadata != null && session.Metadata.TryGetValue("UserId", out var userIdStr) && int.TryParse(userIdStr, out var parsedId))
            {
                userId = parsedId;
            }

            if (userId.HasValue)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", userId.Value);
                parameters.Add("@StripeCustomerId", session.CustomerId);
                parameters.Add("@StripeSubscriptionId", session.SubscriptionId);
                parameters.Add("@SubscriptionStatus", PaymentStatus.Active.ToString());
                parameters.Add("@Action", "UPDATE_SUBSCRIPTION");
                await connection.ExecuteAsync("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                _logger.LogInformation("Updated User {UserId} subscription to Active from CheckoutSessionCompleted", userId.Value);
            }
        }

        private async Task HandleInvoicePaymentSucceededAsync(IDbConnection connection, Invoice invoice)
        {
            var subId = invoice.SubscriptionId;
            var custId = invoice.CustomerId;

            if (string.IsNullOrEmpty(subId) && string.IsNullOrEmpty(custId))
                return;

            var findParams = new DynamicParameters();
            findParams.Add("@Action", "SELECT_ALL");
            var allUsers = await connection.QueryAsync<UserDto>("sp_UserCRUD", findParams, commandType: CommandType.StoredProcedure);
            var matchedUser = allUsers.FirstOrDefault(u =>
                (!string.IsNullOrEmpty(subId) && u.StripeSubscriptionId == subId) ||
                (!string.IsNullOrEmpty(custId) && u.StripeCustomerId == custId));

            if (matchedUser != null)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", matchedUser.UserId);
                parameters.Add("@StripeCustomerId", matchedUser.StripeCustomerId ?? custId);
                parameters.Add("@StripeSubscriptionId", matchedUser.StripeSubscriptionId ?? subId);
                parameters.Add("@SubscriptionStatus", PaymentStatus.Active.ToString());
                parameters.Add("@Action", "UPDATE_SUBSCRIPTION");
                await connection.ExecuteAsync("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                _logger.LogInformation("User {UserId} subscription marked Active after successful invoice payment", matchedUser.UserId);
            }
        }

        private async Task HandleInvoicePaymentFailedAsync(IDbConnection connection, Invoice invoice)
        {
            var subId = invoice.SubscriptionId;
            var custId = invoice.CustomerId;

            if (string.IsNullOrEmpty(subId) && string.IsNullOrEmpty(custId))
                return;

            var findParams = new DynamicParameters();
            findParams.Add("@Action", "SELECT_ALL");
            var allUsers = await connection.QueryAsync<UserDto>("sp_UserCRUD", findParams, commandType: CommandType.StoredProcedure);
            var matchedUser = allUsers.FirstOrDefault(u =>
                (!string.IsNullOrEmpty(subId) && u.StripeSubscriptionId == subId) ||
                (!string.IsNullOrEmpty(custId) && u.StripeCustomerId == custId));

            if (matchedUser != null)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", matchedUser.UserId);
                parameters.Add("@StripeCustomerId", matchedUser.StripeCustomerId ?? custId);
                parameters.Add("@StripeSubscriptionId", matchedUser.StripeSubscriptionId ?? subId);
                parameters.Add("@SubscriptionStatus", PaymentStatus.PastDue.ToString());
                parameters.Add("@Action", "UPDATE_SUBSCRIPTION");
                await connection.ExecuteAsync("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                _logger.LogWarning("User {UserId} subscription marked PastDue due to failed invoice payment", matchedUser.UserId);
            }
        }

        public async Task<ResponseDto> GetAllUsersAsync()
        {
            var responseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL");

                var users = await connection.QueryAsync<UserDto>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                responseDto.Status = 200;
                responseDto.ResponseMsg = "Users retrieved successfully.";
                responseDto.Data = users;
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = $"Server error: {ex.Message}";
                return responseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        private static string MapStripeSubscriptionStatus(string? stripeStatus)
        {
            return (stripeStatus?.ToLower()) switch
            {
                "active" => PaymentStatus.Active.ToString(),
                "trialing" => PaymentStatus.Trialing.ToString(),
                "past_due" => PaymentStatus.PastDue.ToString(),
                "unpaid" => PaymentStatus.Unpaid.ToString(),
                "canceled" => PaymentStatus.Canceled.ToString(),
                "incomplete" => PaymentStatus.Incomplete.ToString(),
                "incomplete_expired" => PaymentStatus.Expired.ToString(),
                _ => PaymentStatus.Pending.ToString()
            };
        }
    }
}
