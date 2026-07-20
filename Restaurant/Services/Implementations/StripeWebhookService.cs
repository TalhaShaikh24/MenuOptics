using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using Stripe;
using System.Data;

namespace MenuOptics.Services
{
    public class StripeWebhookService : IStripeWebhookService
    {
        private readonly DapperContext _context;

        public StripeWebhookService(DapperContext context)
        {
            _context = context;
        }

        public async Task<(int StatusCode, object? Body)> HandleWebhookAsync(string json, string stripeSignature)
        {
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    stripeSignature,
                    "whsec_5faaa8f893e8ee04fe332d778da9a4b4807614c9f57791447440cfdcb58bca33" // Replace with your Stripe webhook secret
                );

                Console.WriteLine(stripeEvent.Type);
                using var connection = _context.CreateConnection();
                connection.Open();
                try
                {
                    if (stripeEvent.Type == Events.CustomerSubscriptionUpdated || stripeEvent.Type == Events.CustomerSubscriptionDeleted)
                    {
                        var subscription = stripeEvent.Data.Object as Subscription;
                        var parameters = new DynamicParameters();
                        parameters.Add("@StripeSubscriptionId", subscription.Id);
                        parameters.Add("@SubscriptionStatus", subscription.Status == "active" ? "Active" : "Expired");
                        parameters.Add("@UserId", subscription.Status == "active" ? "Active" : "Expired");
                        parameters.Add("@Action", "UPDATE_SUBSCRIPTION");

                        await connection.ExecuteAsync("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                    }
                }
                finally
                {
                    connection.Close();
                }

                return (200, null);
            }
            catch (StripeException ex)
            {
                return (400, new { error = ex.Message });
            }
        }

        public async Task<Response> GetAllUsersAsync()
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL");

                var users = await connection.QueryAsync<UserDTO>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                response.Status = 200;
                response.ResponseMsg = "Users retrieved successfully.";
                response.Data = users;
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
