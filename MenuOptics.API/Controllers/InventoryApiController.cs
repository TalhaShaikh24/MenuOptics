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
    [Authorize(Roles = "Restaurant")]
    [Route("api/[controller]")]
    [SubscriptionRequired]
    [ApiController]
    public class InventoryApiController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;
        private readonly IActivityLogService _activityLog;

        public InventoryApiController(IInventoryService inventoryService,IActivityLogService activityLog)
        {
            _inventoryService = inventoryService;
            _activityLog = activityLog;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

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

        [HttpGet("GetInventoryInvoices")]
        public async Task<IActionResult> GetInventoryInvoices()
        {
            var responseData = await _inventoryService.GetInventoryInvoicesAsync(CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPost("CreateInvoice")]
        public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDto
                {
                    Status = 400,
                    ResponseMsg = "Invalid input."
                });
            }

            var responseData = await _inventoryService.CreateInvoiceAsync(model, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPut("UpdateInvoice")]
        public async Task<IActionResult> UpdateInvoice([FromBody] UpdateInvoiceRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDto
                {
                    Status = 400,
                    ResponseMsg = "Invalid input."
                });
            }

            var responseData = await _inventoryService.UpdateInvoiceAsync(model, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }
    }
}
