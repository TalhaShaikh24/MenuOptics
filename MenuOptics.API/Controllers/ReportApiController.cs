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
    public class ReportApiController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportApiController(IReportService reportService)
        {
            _reportService = reportService;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpPost("CogsReport")]
        public async Task<IActionResult> CogsReport([FromBody] CogsReportRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDto
                {
                    Status = 400,
                    ResponseMsg = "Invalid input."
                });
            }

            var responseData = await _reportService.CogsReportAsync(model, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }
    }
}
