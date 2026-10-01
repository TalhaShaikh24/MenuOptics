using MenuOptics.Controllers.Api;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace MenuOptics.Tests.Controllers
{
    public class MenuApiControllerTests
    {
        private readonly Mock<IMenuService> _mockService;
        private readonly MenuApiController _controller;

        public MenuApiControllerTests()
        {
            _mockService = new Mock<IMenuService>();
            _controller = new MenuApiController(_mockService.Object);

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

        /* ── GetAllMenuItems ──────────────────────────────────── */

        [Fact]
        public async Task GetAllMenuItems_Returns200_WhenItemsExist()
        {
            _mockService.Setup(s => s.GetAllMenuItemsAsync(null, 1))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.GetAllMenuItems(null) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        /* ── GetMenuItem ──────────────────────────────────────── */

        [Fact]
        public async Task GetMenuItem_Returns200_WhenFound()
        {
            _mockService.Setup(s => s.GetMenuItemAsync(3))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.GetMenuItem(3) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetMenuItem_Returns404_WhenNotFound()
        {
            _mockService.Setup(s => s.GetMenuItemAsync(999))
                        .ReturnsAsync(new ResponseDto { Status = 404 });

            var result = await _controller.GetMenuItem(999) as ObjectResult;

            Assert.Equal(404, result!.StatusCode);
        }

        /* ── CreateMenuItem ───────────────────────────────────── */

        [Fact]
        public async Task CreateMenuItem_Returns201_WhenCreated()
        {
            var model = new Menu { Name = "Burger" };
            _mockService.Setup(s => s.CreateMenuItemAsync(model, "[]", "[]", 1, 1))
                        .ReturnsAsync(new ResponseDto { Status = 201 });

            var result = await _controller.CreateMenuItem(model, "[]", "[]") as ObjectResult;

            Assert.Equal(201, result!.StatusCode);
        }

        /* ── UpdateMenuItem ───────────────────────────────────── */

        [Fact]
        public async Task UpdateMenuItem_Returns200_WhenUpdated()
        {
            var model = new Menu { MenuItemId = 1, Name = "Updated Burger" };
            _mockService.Setup(s => s.UpdateMenuItemAsync(model, "[]", "[]", 1, 1))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.UpdateMenuItem(model, "[]", "[]") as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        /* ── ToggleMenuActive ─────────────────────────────────── */

        [Fact]
        public async Task ToggleMenuActive_Returns200_WhenToggled()
        {
            _mockService.Setup(s => s.ToggleMenuActiveAsync(2, 1, 1))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.ToggleMenuActive(2) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }
    }
}
