using MenuOptics.API.Middleware;
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

        public InventoryApiController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        [HttpGet("GetInventoryInvoices")]
        public async Task<IActionResult> GetInventoryInvoices()
        {
            var responseData = await _inventoryService.GetInventoryInvoicesAsync(CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPost("CreateInvoice")]
        public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceInventoryDTO model)
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
        public async Task<IActionResult> UpdateInvoice([FromBody] UpdateInvoiceInventoryDTO model)
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
