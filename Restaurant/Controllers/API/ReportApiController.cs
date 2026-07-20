using Microsoft.AspNetCore.Mvc;
using MenuOptics.Models;
using MenuOptics.Services;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
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
        public async Task<IActionResult> CogsReport([FromBody] CogsReportRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new Response
                {
                    Status = 400,
                    ResponseMsg = "Invalid input."
                });
            }

            var response = await _reportService.CogsReportAsync(model, CurrentUserId);
            return StatusCode(response.Status, response);
        }
    }
}
