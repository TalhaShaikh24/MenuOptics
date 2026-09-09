using MenuOptics.API.Middleware;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [SubscriptionRequired]
    [ApiController]
    public class InventorySuiteReportApiController : ControllerBase
    {
        private readonly IInventorySuiteReportService _reportService;

        public InventorySuiteReportApiController(IInventorySuiteReportService reportService)
        {
            _reportService = reportService;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpPost("InventorySuiteReport")]
        public async Task<IActionResult> InventorySuiteReport([FromBody] InventorySuiteReportRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDto
                {
                    Status = 400,
                    ResponseMsg = "Invalid input."
                });
            }

            var responseData = await _reportService.InventorySuiteReportAsync(model, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }
    }
}
