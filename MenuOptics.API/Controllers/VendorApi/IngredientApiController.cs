using MenuOptics.API.Middleware;
using MenuOptics.Middleware;
using MenuOptics.Models;
using MenuOptics.Services.VendorApi;
using MenuOptics.Services.VendorApiServices.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MenuOptics.Controllers.VendorApi
{
    [Route("api/[controller]")]
    [SubscriptionRequired]
    [ApiController]
    public class IngredientApiController : ControllerBase
    {
        private readonly IIngredientService _ingredientService;
        private readonly IVendorActivityLogService _vendorActivityLog;
        public IngredientApiController(IIngredientService ingredientService, IVendorActivityLogService activityLog)
        {
            _ingredientService = ingredientService;
            _vendorActivityLog = activityLog;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        private string RequestId => HttpContext.Items[RequestIdMiddleware.RequestIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Authorize(Roles = "Vendor")]
        [IgnoreSubscription]
        [HttpPost("CreateIngredientByVendor")]
        public async Task<IActionResult> CreateIngredientByVendor([FromBody] VendorIngredientDto model)
        {
            if (!ModelState.IsValid)
            {
                var responseData = new ResponseDto
                {
                    Status = 400,
                    ResponseMsg = "Invalid input."
                };
                await _vendorActivityLog.LogAsync(CurrentUserId, RequestId, "POST /api/IngredientApi/CreateIngredientByVendor",
                    System.Text.Json.JsonSerializer.Serialize(model), System.Text.Json.JsonSerializer.Serialize(responseData), "Warning");
                return BadRequest(responseData);
            }

            var (statusCode, body) = await _ingredientService.CreateIngredientByVendorAsync(model, CurrentUserId, RequestId);
            return StatusCode(statusCode, body);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Authorize(Roles = "Vendor")]
        [IgnoreSubscription]
        [HttpPut("UpdateIngredientPrices")]
        public async Task<IActionResult> UpdateIngredientPrices([FromBody] UpdateIngredientPriceDto model)
        {
            var (statusCode, body) = await _ingredientService.UpdateIngredientPricesAsync(model, CurrentUserId, RequestId);
            return StatusCode(statusCode, body);
        }

        // PATCH api/Ingredient/PatchIngredientDetails
        // Updates one or more of: Name, Unit, IsActive. Only provided (non-null) fields are applied.
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Authorize(Roles = "Vendor")]
        [IgnoreSubscription]
        [HttpPatch("PatchIngredientDetails")]
        public async Task<IActionResult> PatchIngredientDetails([FromBody] UpdateIngredientDetailsDto model)
        {
            var (statusCode, body) = await _ingredientService.PatchIngredientDetailsAsync(model, CurrentUserId, RequestId);
            return StatusCode(statusCode, body);
        }

    }
}
