using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Models;
using MenuOptics.Services;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Restaurant")]
    public class CategoryApiController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryApiController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpGet("GetAllCategories")]
        public async Task<IActionResult> GetAllCategories()
        {
            var response = await _categoryService.GetAllCategoriesAsync(CurrentUserId);
            return StatusCode(response.Status, response);
        }

        [HttpGet("GetCategory/{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var response = await _categoryService.GetCategoryAsync(id, CurrentUserId);
            return StatusCode(response.Status, response);
        }

        [HttpPost("CreateCategory")]
        public async Task<IActionResult> CreateCategory([FromBody] Category model)
        {
            var response = await _categoryService.CreateCategoryAsync(model, CurrentUserId);
            return StatusCode(response.Status, response);
        }

        [HttpPut("UpdateCategory")]
        public async Task<IActionResult> UpdateCategory([FromBody] Category model)
        {
            var response = await _categoryService.UpdateCategoryAsync(model, CurrentUserId);
            return StatusCode(response.Status, response);
        }

        [HttpPut("ToggleCategoryActive/{id}")]
        public async Task<IActionResult> ToggleCategoryActive(int id)
        {
            var response = await _categoryService.ToggleCategoryActiveAsync(id, CurrentUserId);
            return StatusCode(response.Status, response);
        }

        [HttpDelete("DeleteCategoryById/{id}")]
        public async Task<IActionResult> DeleteCategoryById(int id)
        {
            var response = await _categoryService.DeleteCategoryByIdAsync(id, CurrentUserId);
            return StatusCode(response.Status, response);
        }
    }
}
