using Microsoft.AspNetCore.Mvc;
using MenuOptics.Services;

[ApiController]
[Route("api/[controller]")]
public class StripeWebhookController : ControllerBase
{
    private readonly IStripeWebhookService _stripeWebhookService;

    public StripeWebhookController(IStripeWebhookService stripeWebhookService)
    {
        _stripeWebhookService = stripeWebhookService;
    }

    [HttpPost("Webhook")]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var (statusCode, body) = await _stripeWebhookService.HandleWebhookAsync(json, Request.Headers["Stripe-Signature"]);
        return body == null ? StatusCode(statusCode) : StatusCode(statusCode, body);
    }

    [HttpGet("GetAllUsers")]
    public async Task<IActionResult> GetAllUsers()
    {
        var response = await _stripeWebhookService.GetAllUsersAsync();
        return StatusCode(response.Status, response);
    }
}
