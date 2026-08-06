using MenuOptics.API.Middleware;
using MenuOptics.Middleware;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [SubscriptionRequired]
    [ApiController]
    public class InventoryApiController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;
        private readonly IActivityLogService _activityLog;

        public InventoryApiController(IInventoryService inventoryService, IActivityLogService activityLog)
        {
            _inventoryService = inventoryService;
            _activityLog = activityLog;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        private string RequestId => HttpContext.Items[RequestIdMiddleware.RequestIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Authorize(Roles = "Vendor")]
        [IgnoreSubscription]
        [HttpPost("InvoiceInventory")]
        public async Task<IActionResult> InvoiceInventory([FromBody] InvoiceInventoryRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                var responseData = new ResponseDto
                {
                    Status = 400,
                    ResponseMsg = "Invalid input.",
                    Data = ModelState
                };
                await _activityLog.LogAsync(null, RequestId, "POST /api/InventoryApi/InvoiceInventory",
                    System.Text.Json.JsonSerializer.Serialize(model), System.Text.Json.JsonSerializer.Serialize(responseData), "Warning");
                return BadRequest(responseData);
            }

            var result = await _inventoryService.InvoiceInventoryAsync(model, CurrentUserId, RequestId);
            return StatusCode(result.Status, result);
        }

        [HttpGet("GetInventory/{id}")]
        public async Task<IActionResult> GetInventory(int id)
        {
            var responseData = await _inventoryService.GetInventoryAsync(id);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpGet("GetAllInventoryByRestaurant/{restaurantId}")]
        public async Task<IActionResult> GetAllInventoryByRestaurant(int restaurantId)
        {
            var responseData = await _inventoryService.GetAllInventoryByRestaurantAsync(restaurantId, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPost("UpdateClosingCount")]
        public async Task<IActionResult> UpdateClosingCount([FromBody] ClosingCountUpdateDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDto
                {
                    Status = 400,
                    ResponseMsg = "Invalid input."
                });
            }

            var responseData = await _inventoryService.UpdateClosingCountAsync(model, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }
    }
}
