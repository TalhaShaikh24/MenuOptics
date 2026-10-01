using MenuOptics.Controllers.Api;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace MenuOptics.Tests.Controllers
{
    public class InventorySuiteReportApiControllerTests
    {
        private readonly Mock<IInventorySuiteReportService> _mockService;
        private readonly InventorySuiteReportApiController _controller;

        public InventorySuiteReportApiControllerTests()
        {
            _mockService = new Mock<IInventorySuiteReportService>();
            _controller = new InventorySuiteReportApiController(_mockService.Object);

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
        public async Task GetInventorySuiteReports_Returns200()
        {
            _mockService.Setup(s => s.GetInventorySuiteReportsAsync(1))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.GetInventorySuiteReports() as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task InventorySuiteReport_Returns200_WhenValid()
        {
            var model = new InventorySuiteReportRequestDto();
            _mockService.Setup(s => s.InventorySuiteReportAsync(model, 1))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.InventorySuiteReport(model) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task InventorySuiteReport_Returns400_WhenModelInvalid()
        {
            _controller.ModelState.AddModelError("field", "Required");
            var model = new InventorySuiteReportRequestDto();

            var result = await _controller.InventorySuiteReport(model) as ObjectResult;

            Assert.Equal(400, result!.StatusCode);
        }

        [Fact]
        public async Task UpdateInventorySuiteReport_Returns200_WhenUpdated()
        {
            var model = new UpdateInventorySuiteReportRequestDto();
            _mockService.Setup(s => s.UpdateInventorySuiteReportAsync(model, 1))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.UpdateInventorySuiteReport(model) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task AddInventorySuiteReport_Returns201_WhenAdded()
        {
            var model = new AddInventorySuiteReportRequestDto();
            _mockService.Setup(s => s.AddInventorySuiteReportAsync(model, 1))
                        .ReturnsAsync(new ResponseDto { Status = 201 });

            var result = await _controller.AddInventorySuiteReport(model) as ObjectResult;

            Assert.Equal(201, result!.StatusCode);
        }
    }
}
