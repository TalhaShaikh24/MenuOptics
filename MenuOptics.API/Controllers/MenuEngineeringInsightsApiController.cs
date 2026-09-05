using MenuOptics.API.Middleware;
using MenuOptics.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [SubscriptionRequired]
    [ApiController]
    public class MenuEngineeringInsightsApiController : ControllerBase
    {
        private readonly IMenuEngineeringInsightsService _menuEngineeringInsightsService;
        private readonly IMenuAiAnalysisService _menuAiAnalysisService;

        public MenuEngineeringInsightsApiController(
            IMenuEngineeringInsightsService menuEngineeringInsightsService,
            IMenuAiAnalysisService menuAiAnalysisService)
        {
            _menuEngineeringInsightsService = menuEngineeringInsightsService;
            _menuAiAnalysisService = menuAiAnalysisService;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpGet("GetAllMenuEngineeringInsightsByUserId")]
        public async Task<IActionResult> GetAllMenuEngineeringInsightsByUserId(DateTime? startDate, DateTime? endDate)
        {
            var responseData = await _menuEngineeringInsightsService.GetAllMenuEngineeringInsightsByUserIdAsync(CurrentUserId, startDate, endDate);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpGet("GetAiAnalysis")]
        [HttpPost("GenerateAiAnalysis")]
        public async Task<IActionResult> GetAiAnalysis([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var responseData = await _menuAiAnalysisService.GenerateMenuAiAnalysisAsync(CurrentUserId, startDate, endDate);
            return StatusCode(responseData.Status, responseData);
        }
    }
}
