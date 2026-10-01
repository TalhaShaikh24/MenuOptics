using MenuOptics.Controllers.Api;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace MenuOptics.Tests.Controllers
{
    public class IngredientApiControllerTests
    {
        private readonly Mock<IIngredientService> _mockService;
        private readonly IngredientApiController _controller;

        public IngredientApiControllerTests()
        {
            _mockService = new Mock<IIngredientService>();
            _controller = new IngredientApiController(_mockService.Object);

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

        /* ── CreateIngredient ─────────────────────────────────── */

        [Fact]
        public async Task CreateIngredient_Returns201_WhenCreated()
        {
            var model = new Ingredient { Name = "Tomato" };
            _mockService.Setup(s => s.CreateIngredientAsync(model, 1, 1))
                        .ReturnsAsync((201, new ResponseDto { Status = 201 }));

            var result = await _controller.CreateIngredient(model) as ObjectResult;

            Assert.Equal(201, result!.StatusCode);
        }

        [Fact]
        public async Task CreateIngredient_Returns400_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("Name", "Required");
            var model = new Ingredient();

            var result = await _controller.CreateIngredient(model) as ObjectResult;

            Assert.Equal(400, result!.StatusCode);
        }

        /* ── UpdateIngredient ─────────────────────────────────── */

        [Fact]
        public async Task UpdateIngredient_Returns200_WhenUpdated()
        {
            var model = new Ingredient { IngredientId = 1, Name = "Updated" };
            _mockService.Setup(s => s.UpdateIngredientAsync(model, 1, 1))
                        .ReturnsAsync((200, new ResponseDto { Status = 200 }));

            var result = await _controller.UpdateIngredient(model) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        /* ── GetIngredient ────────────────────────────────────── */

        [Fact]
        public async Task GetIngredient_Returns200_WhenFound()
        {
            _mockService.Setup(s => s.GetIngredientAsync(5))
                        .ReturnsAsync((200, new ResponseDto { Status = 200 }));

            var result = await _controller.GetIngredient(5) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetIngredient_Returns404_WhenNotFound()
        {
            _mockService.Setup(s => s.GetIngredientAsync(999))
                        .ReturnsAsync((404, new ResponseDto { Status = 404 }));

            var result = await _controller.GetIngredient(999) as ObjectResult;

            Assert.Equal(404, result!.StatusCode);
        }

        /* ── ToggleIngredientActive ───────────────────────────── */

        [Fact]
        public async Task ToggleIngredientActive_Returns200_WhenToggled()
        {
            _mockService.Setup(s => s.ToggleIngredientActiveAsync(3, 1, 1))
                        .ReturnsAsync((200, new ResponseDto { Status = 200 }));

            var result = await _controller.ToggleIngredientActive(3) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }
    }
}
