using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Middleware;
using MenuOptics.Models;
using MenuOptics.Services;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
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
        [HttpPost("InvoiceInventory")]
        public async Task<IActionResult> InvoiceInventory([FromBody] InvoiceInventoryRequest model)
        {
            if (!ModelState.IsValid)
            {
                var response = new Response
                {
                    Status = 400,
                    ResponseMsg = "Invalid input.",
                    Data = ModelState
                };
                await _activityLog.LogAsync(null, RequestId, "POST /api/InventoryApi/InvoiceInventory",
                    System.Text.Json.JsonSerializer.Serialize(model), System.Text.Json.JsonSerializer.Serialize(response), "Warning");
                return BadRequest(response);
            }

            var result = await _inventoryService.InvoiceInventoryAsync(model, CurrentUserId, RequestId);
            return StatusCode(result.Status, result);
        }

        [HttpGet("GetInventory/{id}")]
        public async Task<IActionResult> GetInventory(int id)
        {
            var response = await _inventoryService.GetInventoryAsync(id);
            return StatusCode(response.Status, response);
        }

        [HttpGet("GetAllInventoryByRestaurant/{restaurantId}")]
        public async Task<IActionResult> GetAllInventoryByRestaurant(int restaurantId)
        {
            var response = await _inventoryService.GetAllInventoryByRestaurantAsync(restaurantId, CurrentUserId);
            return StatusCode(response.Status, response);
        }

        [HttpPost("UpdateClosingCount")]
        public async Task<IActionResult> UpdateClosingCount([FromBody] ClosingCountUpdateDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new Response
                {
                    Status = 400,
                    ResponseMsg = "Invalid input."
                });
            }

            var response = await _inventoryService.UpdateClosingCountAsync(model, CurrentUserId);
            return StatusCode(response.Status, response);
        }
    }
}
