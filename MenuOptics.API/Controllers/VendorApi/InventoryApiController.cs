using MenuOptics.API.Middleware;
using MenuOptics.Models;
using MenuOptics.Models.DTOs;
using MenuOptics.Services.VendorApi;
using MenuOptics.Services.VendorApiServices.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MenuOptics.Controllers.VendorApi
{
    [Authorize]
    [Route("api/[controller]")]
    [SubscriptionRequired]
    [ApiController]
    public class InventoryApiController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;
        private readonly IVendorActivityLogService _vendorActivityLog;

        public InventoryApiController(IInventoryService inventoryService, IVendorActivityLogService activityLog)
        {
            _inventoryService = inventoryService;
            _vendorActivityLog = activityLog;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [Authorize(Roles = "Vendor")]
        [IgnoreSubscription]
        [HttpPost("InvoiceInventory")]
        public async Task<IActionResult> InvoiceInventory([FromBody] CreateVendorInvoiceInventoryDTO model)
        {
            if (!ModelState.IsValid)
            {
                var responseData = new ResponseDto
                {
                    Status = 400,
                    ResponseMsg = "Invalid input.",
                    Data = ModelState
                };
                await _vendorActivityLog.LogAsync(CurrentUserId, "POST /api/InventoryApi/InvoiceInventory",
                    System.Text.Json.JsonSerializer.Serialize(model), System.Text.Json.JsonSerializer.Serialize(responseData), "Warning");
                return BadRequest(responseData);
            }

            var result = await _inventoryService.InvoiceInventoryAsync(model, CurrentUserId);
            return StatusCode(result.Status, result);
        }
    }
}
