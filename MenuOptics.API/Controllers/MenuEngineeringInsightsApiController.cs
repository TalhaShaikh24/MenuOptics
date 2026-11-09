using MenuOptics.API.Middleware;
using MenuOptics.Models;
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

        [HttpGet("GetMenuDesignData")]
        public async Task<IActionResult> GetMenuDesignData()
        {
            var responseData = await _menuAiAnalysisService.GetMenuDesignDataAsync(CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPost("GenerateMenuDesigns")]
        public async Task<IActionResult> GenerateMenuDesigns([FromBody] ExtractedMenuDto menu)
        {
            var responseData = await _menuAiAnalysisService.GenerateMenuDesignsAsync(menu);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPost("AnalyzeMenuPdf")]
        [RequestSizeLimit(25_000_000)]
        public async Task<IActionResult> AnalyzeMenuPdf([FromBody] MenuPdfAnalysisRequestDto request)
        {
            var responseData = await _menuAiAnalysisService.AnalyzeMenuPdfAsync(CurrentUserId, request);
            return StatusCode(responseData.Status, responseData);
        }
    }
}
