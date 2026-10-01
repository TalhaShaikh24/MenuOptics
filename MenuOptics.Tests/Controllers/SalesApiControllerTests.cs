using MenuOptics.Controllers.Api;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace MenuOptics.Tests.Controllers
{
    public class SalesApiControllerTests
    {
        private readonly Mock<ISalesService> _mockService;
        private readonly SalesApiController _controller;

        public SalesApiControllerTests()
        {
            _mockService = new Mock<ISalesService>();
            _controller = new SalesApiController(_mockService.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim("ActualUserId", "1"),
                new Claim(ClaimTypes.Role, "Restaurant")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        /* ── GetAllSales ──────────────────────────────────────── */

        [Fact]
        public async Task GetAllSales_Returns200_WhenSalesExist()
        {
            _mockService.Setup(s => s.GetAllSalesAsync(1))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.GetAllSales() as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        /* ── CreateSale ───────────────────────────────────────── */

        [Fact]
        public async Task CreateSale_Returns201_WhenCreated()
        {
            var model = new Sale { Total = 150.00m };
            _mockService.Setup(s => s.CreateSaleAsync(model, 1))
                        .ReturnsAsync(new ResponseDto { Status = 201 });

            var result = await _controller.CreateSale(model) as ObjectResult;

            Assert.Equal(201, result!.StatusCode);
        }

        [Fact]
        public async Task CreateSale_Returns400_WhenInvalidData()
        {
            var model = new Sale();
            _mockService.Setup(s => s.CreateSaleAsync(model, 1))
                        .ReturnsAsync(new ResponseDto { Status = 400 });

            var result = await _controller.CreateSale(model) as ObjectResult;

            Assert.Equal(400, result!.StatusCode);
        }
    }
}
