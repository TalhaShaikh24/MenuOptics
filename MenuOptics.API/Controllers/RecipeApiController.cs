using Microsoft.AspNetCore.Mvc;
using MenuOptics.Models;
using MenuOptics.Services;
using System.Security.Claims;

namespace MenuOptics.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecipeApiController : ControllerBase
    {
        private readonly IRecipeService _recipeService;

        public RecipeApiController(IRecipeService recipeService)
        {
            _recipeService = recipeService;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpPost("CreateRecipe")]
        public async Task<IActionResult> CreateRecipe([FromForm] Recipe model, [FromForm] string ingredients)
        {
            var responseData = await _recipeService.CreateRecipeAsync(model, ingredients, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPut("UpdateRecipe")]
        public async Task<IActionResult> UpdateRecipe([FromForm] Recipe model, [FromForm] string ingredients)
        {
            var responseData = await _recipeService.UpdateRecipeAsync(model, ingredients, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpGet("GetRecipe/{id}")]
        public async Task<IActionResult> GetRecipe(int id)
        {
            var responseData = await _recipeService.GetRecipeAsync(id);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpGet("GetAllRecipes")]
        public async Task<IActionResult> GetAllRecipes()
        {
            var responseData = await _recipeService.GetAllRecipesAsync(CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPut("ToggleRecipeActive/{id}")]
        public async Task<IActionResult> ToggleRecipeActive(int id)
        {
            var responseData = await _recipeService.ToggleRecipeActiveAsync(id, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }
    }
}
