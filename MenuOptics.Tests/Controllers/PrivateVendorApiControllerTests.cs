using MenuOptics.Controllers.Api;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace MenuOptics.Tests.Controllers
{
    public class PrivateVendorApiControllerTests
    {
        private readonly Mock<IPrivateVendorService> _mockService;
        private readonly PrivateVendorApiController _controller;

        public PrivateVendorApiControllerTests()
        {
            _mockService = new Mock<IPrivateVendorService>();
            _controller = new PrivateVendorApiController(_mockService.Object);

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

        [Fact]
        public async Task GetAllPrivateVendors_Returns200()
        {
            _mockService.Setup(s => s.GetAllPrivateVendorsAsync(1))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.GetAllPrivateVendors() as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetPrivateVendor_Returns200_WhenFound()
        {
            _mockService.Setup(s => s.GetPrivateVendorAsync(5, 1))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.GetPrivateVendor(5) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetPrivateVendor_Returns404_WhenNotFound()
        {
            _mockService.Setup(s => s.GetPrivateVendorAsync(999, 1))
                        .ReturnsAsync(new ResponseDto { Status = 404 });

            var result = await _controller.GetPrivateVendor(999) as ObjectResult;

            Assert.Equal(404, result!.StatusCode);
        }

        [Fact]
        public async Task CreatePrivateVendor_Returns201_WhenCreated()
        {
            var model = new PrivateVendor { Name = "Local Supplier" };
            _mockService.Setup(s => s.CreatePrivateVendorAsync(model, 1, 1))
                        .ReturnsAsync(new ResponseDto { Status = 201 });

            var result = await _controller.CreatePrivateVendor(model) as ObjectResult;

            Assert.Equal(201, result!.StatusCode);
        }

        [Fact]
        public async Task UpdatePrivateVendor_Returns200_WhenUpdated()
        {
            var model = new PrivateVendor { PrivateVendorId = 1, Name = "Updated Supplier" };
            _mockService.Setup(s => s.UpdatePrivateVendorAsync(model, 1, 1))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.UpdatePrivateVendor(model) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task TogglePrivateVendorActive_Returns200_WhenToggled()
        {
            _mockService.Setup(s => s.TogglePrivateVendorActiveAsync(2, 1, 1))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.TogglePrivateVendorActive(2) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }
    }
}
