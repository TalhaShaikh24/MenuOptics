using MenuOptics.Controllers.Api;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace MenuOptics.Tests.Controllers
{
    public class AnalyticsApiControllerTests
    {
        private readonly Mock<IAnalyticsService> _mockService;
        private readonly AnalyticsApiController _controller;

        public AnalyticsApiControllerTests()
        {
            _mockService = new Mock<IAnalyticsService>();
            _controller = new AnalyticsApiController(_mockService.Object);

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
        public async Task GetAnalyticsDashboard_Returns200_WithNoDates()
        {
            _mockService.Setup(s => s.GetAnalyticsDashboardAsync(1, null, null))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.GetAnalyticsDashboard(null, null) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetAnalyticsDashboard_Returns200_WithDateRange()
        {
            var start = new DateTime(2026, 1, 1);
            var end = new DateTime(2026, 9, 30);
            _mockService.Setup(s => s.GetAnalyticsDashboardAsync(1, start, end))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.GetAnalyticsDashboard(start, end) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetAnalyticsDashboard_Returns500_WhenServiceFails()
        {
            _mockService.Setup(s => s.GetAnalyticsDashboardAsync(1, null, null))
                        .ReturnsAsync(new ResponseDto { Status = 500 });

            var result = await _controller.GetAnalyticsDashboard(null, null) as ObjectResult;

            Assert.Equal(500, result!.StatusCode);
        }
    }
}
