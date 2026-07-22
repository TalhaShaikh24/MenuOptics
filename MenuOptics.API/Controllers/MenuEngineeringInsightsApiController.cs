using Microsoft.AspNetCore.Mvc;
using MenuOptics.Services;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuEngineeringInsightsApiController : ControllerBase
    {
        private readonly IMenuEngineeringInsightsService _menuEngineeringInsightsService;

        public MenuEngineeringInsightsApiController(IMenuEngineeringInsightsService menuEngineeringInsightsService)
        {
            _menuEngineeringInsightsService = menuEngineeringInsightsService;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpGet("GetAllMenuEngineeringInsightsByUserId")]
        public async Task<IActionResult> GetAllMenuEngineeringInsightsByUserId(DateTime? startDate, DateTime? endDate)
        {
            var responseData = await _menuEngineeringInsightsService.GetAllMenuEngineeringInsightsByUserIdAsync(CurrentUserId, startDate, endDate);
            return StatusCode(responseData.Status, responseData);
        }
    }
}
