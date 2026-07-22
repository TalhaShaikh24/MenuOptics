using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Models;
using MenuOptics.Services;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesApiController : ControllerBase
    {
        private readonly ISalesService _salesService;

        public SalesApiController(ISalesService salesService)
        {
            _salesService = salesService;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpPost("CreateSale")]
        [Authorize]
        public async Task<IActionResult> CreateSale([FromBody] Sale model)
        {
            var response = await _salesService.CreateSaleAsync(model, CurrentUserId);
            return StatusCode(response.Status, response);
        }

        [HttpGet("GetAllSales")]
        [Authorize]
        public async Task<IActionResult> GetAllSales()
        {
            var response = await _salesService.GetAllSalesAsync();
            return StatusCode(response.Status, response);
        }

        [HttpPost("ImportSales")]
        [Authorize]
        public async Task<IActionResult> ImportSales(IFormFile file)
        {
            var response = await _salesService.ImportSalesAsync(file, CurrentUserId);
            return StatusCode(response.Status, response);
        }
    }
}
