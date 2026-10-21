using Microsoft.AspNetCore.Mvc;
using MenuOptics.Services;

[ApiController]
[Route("api/[controller]")]
public class StripeWebhookController : ControllerBase
{
    private readonly IStripeWebhookService _stripeWebhookService;
    private readonly IUserService _userService;

    public StripeWebhookController(IStripeWebhookService stripeWebhookService, IUserService userService)
    {   
        _stripeWebhookService = stripeWebhookService;
        _userService = userService;
    }

    [HttpPost("Webhook")]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var (statusCode, body) = await _stripeWebhookService.HandleWebhookAsync(json, Request.Headers["Stripe-Signature"]);
        return body == null ? StatusCode(statusCode) : StatusCode(statusCode, body);
    }
}
