using MenuOptics.Controllers.Api;
using MenuOptics.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using MenuOptics.Models;

namespace MenuOptics.Tests.Controllers
{
    public class MenuEngineeringInsightsApiControllerTests
    {
        private readonly Mock<IMenuEngineeringInsightsService> _mockInsightsService;
        private readonly Mock<IMenuAiAnalysisService> _mockAiService;
        private readonly MenuEngineeringInsightsApiController _controller;

        public MenuEngineeringInsightsApiControllerTests()
        {
            _mockInsightsService = new Mock<IMenuEngineeringInsightsService>();
            _mockAiService = new Mock<IMenuAiAnalysisService>();
            _controller = new MenuEngineeringInsightsApiController(
                _mockInsightsService.Object,
                _mockAiService.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Restaurant")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        [Fact]
        public async Task GetAllMenuEngineeringInsights_Returns200_WithNoDates()
        {
            _mockInsightsService.Setup(s => s.GetAllMenuEngineeringInsightsByUserIdAsync(1, null, null))
                                .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.GetAllMenuEngineeringInsightsByUserId(null, null) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetAllMenuEngineeringInsights_Returns200_WithDateRange()
        {
            var start = new DateTime(2026, 1, 1);
            var end = new DateTime(2026, 9, 30);
            _mockInsightsService.Setup(s => s.GetAllMenuEngineeringInsightsByUserIdAsync(1, start, end))
                                .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.GetAllMenuEngineeringInsightsByUserId(start, end) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetAiAnalysis_Returns200_WhenGenerated()
        {
            _mockAiService.Setup(s => s.GenerateMenuAiAnalysisAsync(1, null, null))
                          .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.GetAiAnalysis(null, null) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetAiAnalysis_Returns500_WhenAiFails()
        {
            _mockAiService.Setup(s => s.GenerateMenuAiAnalysisAsync(1, null, null))
                          .ReturnsAsync(new ResponseDto { Status = 500 });

            var result = await _controller.GetAiAnalysis(null, null) as ObjectResult;

            Assert.Equal(500, result!.StatusCode);
        }
    }
}
