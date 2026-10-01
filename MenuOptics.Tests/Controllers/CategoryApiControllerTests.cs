using MenuOptics.Controllers.Api;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace MenuOptics.Tests.Controllers
{
    public class CategoryApiControllerTests
    {
        private readonly Mock<ICategoryService> _mockService;
        private readonly CategoryApiController _controller;

        public CategoryApiControllerTests()
        {
            _mockService = new Mock<ICategoryService>();
            _controller = new CategoryApiController(_mockService.Object);

            // Setup fake authenticated user (UserId = 1, ActualUserId = 1)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim("ActualUserId", "1"),
                new Claim(ClaimTypes.Role, "Restaurant")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        /* ── GetAllCategories ─────────────────────────────────── */

        [Fact]
        public async Task GetAllCategories_Returns200_WhenServiceSucceeds()
        {
            // Arrange
            var expected = new ResponseDto { Status = 200, ResponseMsg = "Success" };
            _mockService.Setup(s => s.GetAllCategoriesAsync(1)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetAllCategories() as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetAllCategories_Returns500_WhenServiceFails()
        {
            // Arrange
            var expected = new ResponseDto { Status = 500, ResponseMsg = "Internal error" };
            _mockService.Setup(s => s.GetAllCategoriesAsync(1)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetAllCategories() as ObjectResult;

            // Assert
            Assert.Equal(500, result!.StatusCode);
        }

        /* ── GetCategory ──────────────────────────────────────── */

        [Fact]
        public async Task GetCategory_Returns200_WhenFound()
        {
            // Arrange
            var expected = new ResponseDto { Status = 200 };
            _mockService.Setup(s => s.GetCategoryAsync(5, 1)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetCategory(5) as ObjectResult;

            // Assert
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetCategory_Returns404_WhenNotFound()
        {
            // Arrange
            var expected = new ResponseDto { Status = 404, ResponseMsg = "Not found" };
            _mockService.Setup(s => s.GetCategoryAsync(99, 1)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetCategory(99) as ObjectResult;

            // Assert
            Assert.Equal(404, result!.StatusCode);
        }

        /* ── CreateCategory ───────────────────────────────────── */

        [Fact]
        public async Task CreateCategory_Returns201_WhenCreated()
        {
            // Arrange
            var model = new Category { Name = "Beverages" };
            var expected = new ResponseDto { Status = 201, ResponseMsg = "Created" };
            _mockService.Setup(s => s.CreateCategoryAsync(model, 1, 1)).ReturnsAsync(expected);

            // Act
            var result = await _controller.CreateCategory(model) as ObjectResult;

            // Assert
            Assert.Equal(201, result!.StatusCode);
        }

        [Fact]
        public async Task CreateCategory_Returns400_WhenModelIsInvalid()
        {
            // Arrange
            var model = new Category(); // empty / invalid
            var expected = new ResponseDto { Status = 400, ResponseMsg = "Bad request" };
            _mockService.Setup(s => s.CreateCategoryAsync(model, 1, 1)).ReturnsAsync(expected);

            // Act
            var result = await _controller.CreateCategory(model) as ObjectResult;

            // Assert
            Assert.Equal(400, result!.StatusCode);
        }

        /* ── UpdateCategory ───────────────────────────────────── */

        [Fact]
        public async Task UpdateCategory_Returns200_WhenUpdated()
        {
            // Arrange
            var model = new Category { CategoryId = 1, Name = "Updated" };
            var expected = new ResponseDto { Status = 200, ResponseMsg = "Updated" };
            _mockService.Setup(s => s.UpdateCategoryAsync(model, 1, 1)).ReturnsAsync(expected);

            // Act
            var result = await _controller.UpdateCategory(model) as ObjectResult;

            // Assert
            Assert.Equal(200, result!.StatusCode);
        }

        /* ── ToggleCategoryActive ─────────────────────────────── */

        [Fact]
        public async Task ToggleCategoryActive_Returns200_WhenToggled()
        {
            // Arrange
            var expected = new ResponseDto { Status = 200, ResponseMsg = "Toggled" };
            _mockService.Setup(s => s.ToggleCategoryActiveAsync(3, 1, 1)).ReturnsAsync(expected);

            // Act
            var result = await _controller.ToggleCategoryActive(3) as ObjectResult;

            // Assert
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task ToggleCategoryActive_Returns404_WhenCategoryNotFound()
        {
            // Arrange
            var expected = new ResponseDto { Status = 404, ResponseMsg = "Not found" };
            _mockService.Setup(s => s.ToggleCategoryActiveAsync(999, 1, 1)).ReturnsAsync(expected);

            // Act
            var result = await _controller.ToggleCategoryActive(999) as ObjectResult;

            // Assert
            Assert.Equal(404, result!.StatusCode);
        }
    }
}
