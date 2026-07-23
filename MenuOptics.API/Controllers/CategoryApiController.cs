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
            var responseData = await _categoryService.GetAllCategoriesAsync(CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpGet("GetCategory/{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var responseData = await _categoryService.GetCategoryAsync(id, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPost("CreateCategory")]
        public async Task<IActionResult> CreateCategory([FromBody] Category model)
        {
            var responseData = await _categoryService.CreateCategoryAsync(model, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPut("UpdateCategory")]
        public async Task<IActionResult> UpdateCategory([FromBody] Category model)
        {
            var responseData = await _categoryService.UpdateCategoryAsync(model, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPut("ToggleCategoryActive/{id}")]
        public async Task<IActionResult> ToggleCategoryActive(int id)
        {
            var responseData = await _categoryService.ToggleCategoryActiveAsync(id, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

    }
}
