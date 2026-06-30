using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Data;
using Restaurant.Models;
using Stripe;
using Stripe.Checkout;
using Stripe.Forwarding;
using System.Data;

[ApiController]
[Route("api/[controller]")]
public class StripeWebhookController : ControllerBase
{
    
    private readonly DapperContext _context;

    public StripeWebhookController(DapperContext context)
    {
        _context = context;
    }


    [HttpPost("Webhook")]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
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

            return Ok();
        }
        catch (StripeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }


    [HttpGet("GetAllUsers")]
    public async Task<IActionResult> GetAllUsers()
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
            return Ok(response);
        }
        catch (Exception ex)
        {
            response.Status = 500;
            response.ResponseMsg = $"Server error: {ex.Message}";
            return StatusCode(500, response);
        }
        finally
        {
            connection.Close();
        }
    }

}
