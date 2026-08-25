using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using MenuOptics.Models.DTOs.Subscription;
using MenuOptics.Models.Enums;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using System.Data;

namespace MenuOptics.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly DapperContext _context;
        private readonly IConfiguration _configuration;

        public SubscriptionService(DapperContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            var stripeKey = _configuration["Stripe:SecretKey"] 
                ?? "sk_test_51M9O4qKR3yBF1l8fZJ5Ll6Q7x0V0ec9i8rgqi8FMKtHTqSx3WYMvdCeOpuOdpImJ6OmhTiq6DDUhB83zumFikT2700QEmhSeSP";
            StripeConfiguration.ApiKey = stripeKey;
        }

        public async Task<ResponseDto> GetSubscriptionDetailsAsync(int effectiveUserId, int actualUserId, bool isParent)
        {
            var responseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                // Fetch effective user (parent restaurant if staff member)
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", effectiveUserId);
                parameters.Add("@Action", "SELECT");

                var user = await connection.QueryFirstOrDefaultAsync<UserDto>(
                    "sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (user == null)
                {
                    responseDto.Status = 404;
                    responseDto.ResponseMsg = "Restaurant account not found.";
                    return responseDto;
                }

                // If actual user is a staff member, fetch parent name for context
                string? parentRestaurantName = null;
                if (!isParent && user.RestaurantName != null)
                {
                    parentRestaurantName = user.RestaurantName;
                }

                var dto = new SubscriptionDetailsDto
                {
                    UserId = user.UserId,
                    RestaurantName = user.RestaurantName,
                    Email = user.Email,
                    Plan = user.SubscriptionType ?? "Starter",
                    SubscriptionStatus = user.SubscriptionStatus ?? PaymentStatus.Pending.ToString(),
                    StripeCustomerId = user.StripeCustomerId,
                    StripeSubscriptionId = user.StripeSubscriptionId,
                    IsParent = isParent,
                    ParentId = user.ParentId,
                    ParentRestaurantName = parentRestaurantName,
                    IsActiveSubscription = string.Equals(user.SubscriptionStatus, PaymentStatus.Active.ToString(), StringComparison.OrdinalIgnoreCase) ||
                                           string.Equals(user.SubscriptionStatus, PaymentStatus.Trialing.ToString(), StringComparison.OrdinalIgnoreCase)
                };

                // Retrieve live Stripe subscription details if subscription ID is present
                if (!string.IsNullOrEmpty(user.StripeSubscriptionId))
                {
                    try
                    {
                        var stripeSubService = new Stripe.SubscriptionService();
                        var getOptions = new SubscriptionGetOptions
                        {
                            Expand = new List<string> { "latest_invoice", "default_payment_method" }
                        };
                        var stripeSub = await stripeSubService.GetAsync(user.StripeSubscriptionId, getOptions);

                        if (stripeSub != null)
                        {
                            dto.CurrentPeriodStart = stripeSub.CurrentPeriodStart;
                            dto.CurrentPeriodEnd = stripeSub.CurrentPeriodEnd;
                            dto.CancelAtPeriodEnd = stripeSub.CancelAtPeriodEnd;

                            var item = stripeSub.Items?.Data?.FirstOrDefault();
                            if (item?.Price != null)
                            {
                                dto.Amount = (item.Price.UnitAmount ?? 0) / 100M;
                                dto.Currency = item.Price.Currency?.ToUpper();
                                dto.Interval = item.Price.Recurring?.Interval;
                            }

                            if (stripeSub.LatestInvoice != null)
                            {
                                dto.LatestInvoiceStatus = stripeSub.LatestInvoice.Status;
                            }

                            if (stripeSub.DefaultPaymentMethod is PaymentMethod pm && pm.Card != null)
                            {
                                dto.PaymentMethodBrand = pm.Card.Brand;
                                dto.PaymentMethodLast4 = pm.Card.Last4;
                            }

                            // Sync status if different
                            var mappedStatus = MapStripeStatus(stripeSub.Status);
                            if (!string.Equals(user.SubscriptionStatus, mappedStatus, StringComparison.OrdinalIgnoreCase))
                            {
                                var updateParams = new DynamicParameters();
                                updateParams.Add("@UserId", user.UserId);
                                updateParams.Add("@StripeCustomerId", user.StripeCustomerId);
                                updateParams.Add("@StripeSubscriptionId", user.StripeSubscriptionId);
                                updateParams.Add("@SubscriptionStatus", mappedStatus);
                                updateParams.Add("@Action", "UPDATE_SUBSCRIPTION");
                                await connection.ExecuteAsync("sp_UserCRUD", updateParams, commandType: CommandType.StoredProcedure);

                                dto.SubscriptionStatus = mappedStatus;
                                dto.IsActiveSubscription = string.Equals(mappedStatus, PaymentStatus.Active.ToString(), StringComparison.OrdinalIgnoreCase) ||
                                                           string.Equals(mappedStatus, PaymentStatus.Trialing.ToString(), StringComparison.OrdinalIgnoreCase);
                            }
                        }
                    }
                    catch (StripeException)
                    {
                        // Fallback to local DB record if Stripe API call encounters an issue
                    }
                }

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Subscription details retrieved successfully.";
                responseDto.Data = dto;
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = $"Error retrieving subscription: {ex.Message}";
                return responseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> CancelSubscriptionAsync(int effectiveUserId, int actualUserId, bool isParent)
        {
            var responseDto = new ResponseDto();

            if (!isParent)
            {
                responseDto.Status = 403;
                responseDto.ResponseMsg = "Only the restaurant owner can manage or cancel the subscription.";
                return responseDto;
            }

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", effectiveUserId);
                parameters.Add("@Action", "SELECT");

                var user = await connection.QueryFirstOrDefaultAsync<UserDto>(
                    "sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (user == null)
                {
                    responseDto.Status = 404;
                    responseDto.ResponseMsg = "User account not found.";
                    return responseDto;
                }

                if (!string.IsNullOrEmpty(user.StripeSubscriptionId))
                {
                    try
                    {
                        var stripeSubService = new Stripe.SubscriptionService();
                        await stripeSubService.CancelAsync(user.StripeSubscriptionId);
                    }
                    catch (StripeException ex)
                    {
                        // If already canceled in Stripe, proceed to update DB
                        if (!ex.Message.Contains("No such subscription") && !ex.Message.Contains("canceled"))
                        {
                            responseDto.Status = 400;
                            responseDto.ResponseMsg = $"Stripe error: {ex.Message}";
                            return responseDto;
                        }
                    }
                }

                var updateParams = new DynamicParameters();
                updateParams.Add("@UserId", user.UserId);
                updateParams.Add("@StripeCustomerId", user.StripeCustomerId);
                updateParams.Add("@StripeSubscriptionId", user.StripeSubscriptionId);
                updateParams.Add("@SubscriptionType", user.SubscriptionType);
                updateParams.Add("@SubscriptionStatus", PaymentStatus.Canceled.ToString());
                updateParams.Add("@Action", "UPDATE_SUBSCRIPTION");

                await connection.ExecuteAsync("sp_UserCRUD", updateParams, commandType: CommandType.StoredProcedure);

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Subscription canceled successfully.";
                responseDto.Data = new { SubscriptionStatus = PaymentStatus.Canceled.ToString() };
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = $"Error canceling subscription: {ex.Message}";
                return responseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> CreateCheckoutSessionAsync(int effectiveUserId, string plan, string requestBaseUrl)
        {
            var responseDto = new ResponseDto();

            try
            {
                var normalizedPlan = string.IsNullOrWhiteSpace(plan) ? "professional" : plan.ToLower();
                var proPrice = _configuration["Stripe:ProfessionalPriceId"] ?? "price_1Rt2LNKR3yBF1l8f4ww3GlNh";
                var starterPrice = _configuration["Stripe:StarterPriceId"] ?? proPrice;

                var priceId = normalizedPlan switch
                {
                    "starter" => starterPrice,
                    _ => proPrice
                };

                var webBaseUrl = (!string.IsNullOrWhiteSpace(requestBaseUrl) && !requestBaseUrl.Contains("7104"))
                    ? requestBaseUrl.TrimEnd('/')
                    : (_configuration["WebUrl"] ?? "https://localhost:7103").TrimEnd('/');

                var successUrl = $"{webBaseUrl}/Payment/successpayment?session_id={{CHECKOUT_SESSION_ID}}&user_id={effectiveUserId}";
                var cancelUrl = $"{webBaseUrl}/Subscription";

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
                        { "UserId", effectiveUserId.ToString() },
                        { "Plan", normalizedPlan }
                    },
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Checkout session created successfully.";
                responseDto.Data = new
                {
                    sessionId = session.Id,
                    url = session.Url
                };
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = $"Error creating checkout session: {ex.Message}";
                return responseDto;
            }
        }

        public async Task<ResponseDto> RetryPaymentAsync(int effectiveUserId, int actualUserId, bool isParent, string requestBaseUrl)
        {
            var responseDto = new ResponseDto();

            if (!isParent)
            {
                responseDto.Status = 403;
                responseDto.ResponseMsg = "Only the restaurant owner can retry or manage payments.";
                return responseDto;
            }

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", effectiveUserId);
                parameters.Add("@Action", "SELECT");

                var user = await connection.QueryFirstOrDefaultAsync<UserDto>(
                    "sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (user == null)
                {
                    responseDto.Status = 404;
                    responseDto.ResponseMsg = "User account not found.";
                    return responseDto;
                }

                var plan = user.SubscriptionType ?? "starter";
                return await CreateCheckoutSessionAsync(effectiveUserId, plan, requestBaseUrl);
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = $"Error creating retry payment session: {ex.Message}";
                return responseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        private static string MapStripeStatus(string? stripeStatus)
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
