using MenuOptics.API.Middleware;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [SubscriptionRequired]
    [ApiController]
    public class SalesApiController : ControllerBase
    {
        private readonly ISalesService _salesService;

        public SalesApiController(ISalesService salesService)
        {
            _salesService = salesService;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        private int ActualUserId => int.Parse(User.FindFirst("ActualUserId")?.Value ?? "0");

        [HttpPost("CreateSale")]
        [Authorize]
        public async Task<IActionResult> CreateSale([FromBody] Sale model)
        {
            var responseData = await _salesService.CreateSaleAsync(model, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPut("UpdateSale")]
        [Authorize]
        public async Task<IActionResult> UpdateSale([FromBody] Sale model)
        {
            var responseData = await _salesService.UpdateSaleAsync(model, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpGet("GetAllSales")]
        [Authorize]
        public async Task<IActionResult> GetAllSales()
        {
            var responseData = await _salesService.GetAllSalesAsync(CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPost("ImportSales")]
        [Authorize]
        public async Task<IActionResult> ImportSales(IFormFile file, [FromForm] DateTime startDate, [FromForm] DateTime endDate)
        {
            var responseData = await _salesService.ImportSalesAsync(file, startDate, endDate, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }
    }
}
