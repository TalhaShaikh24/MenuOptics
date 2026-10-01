using MenuOptics.Controllers.API;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace MenuOptics.Tests.Controllers
{
    public class RecipeApiControllerTests
    {
        private readonly Mock<IRecipeService> _mockService;
        private readonly RecipeApiController _controller;

        public RecipeApiControllerTests()
        {
            _mockService = new Mock<IRecipeService>();
            _controller = new RecipeApiController(_mockService.Object);

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

        /* ── GetAllRecipes ────────────────────────────────────── */

        [Fact]
        public async Task GetAllRecipes_Returns200_WhenRecipesExist()
        {
            var expected = new ResponseDto { Status = 200 };
            _mockService.Setup(s => s.GetAllRecipesAsync(1)).ReturnsAsync(expected);

            var result = await _controller.GetAllRecipes() as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        /* ── GetRecipe ────────────────────────────────────────── */

        [Fact]
        public async Task GetRecipe_Returns200_WhenFound()
        {
            var expected = new ResponseDto { Status = 200 };
            _mockService.Setup(s => s.GetRecipeAsync(3)).ReturnsAsync(expected);

            var result = await _controller.GetRecipe(3) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetRecipe_Returns404_WhenNotFound()
        {
            var expected = new ResponseDto { Status = 404 };
            _mockService.Setup(s => s.GetRecipeAsync(999)).ReturnsAsync(expected);

            var result = await _controller.GetRecipe(999) as ObjectResult;

            Assert.Equal(404, result!.StatusCode);
        }

        /* ── CreateRecipe ─────────────────────────────────────── */

        [Fact]
        public async Task CreateRecipe_Returns201_WhenCreated()
        {
            var model = new Recipe { Name = "Pasta" };
            var expected = new ResponseDto { Status = 201 };
            _mockService.Setup(s => s.CreateRecipeAsync(model, "[]", 1, 1)).ReturnsAsync(expected);

            var result = await _controller.CreateRecipe(model, "[]") as ObjectResult;

            Assert.Equal(201, result!.StatusCode);
        }

        /* ── UpdateRecipe ─────────────────────────────────────── */

        [Fact]
        public async Task UpdateRecipe_Returns200_WhenUpdated()
        {
            var model = new Recipe { RecipeId = 1, Name = "Updated Pasta" };
            var expected = new ResponseDto { Status = 200 };
            _mockService.Setup(s => s.UpdateRecipeAsync(model, "[]", 1, 1)).ReturnsAsync(expected);

            var result = await _controller.UpdateRecipe(model, "[]") as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        /* ── ToggleRecipeActive ───────────────────────────────── */

        [Fact]
        public async Task ToggleRecipeActive_Returns200_WhenToggled()
        {
            var expected = new ResponseDto { Status = 200 };
            _mockService.Setup(s => s.ToggleRecipeActiveAsync(2, 1, 1)).ReturnsAsync(expected);

            var result = await _controller.ToggleRecipeActive(2) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }
    }
}
