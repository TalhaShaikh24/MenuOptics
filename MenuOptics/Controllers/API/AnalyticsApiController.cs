using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Services;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Restaurant")]
    public class AnalyticsApiController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsApiController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpGet("GetAnalyticsDashboard")]
        public async Task<IActionResult> GetAnalyticsDashboard(DateTime? startDate, DateTime? endDate)
        {
            var response = await _analyticsService.GetAnalyticsDashboardAsync(CurrentUserId, startDate, endDate);
            return StatusCode(response.Status, response);
        }
    }
}
