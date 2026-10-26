using MenuOptics.API.Middleware;
using MenuOptics.Models.DTOs.Category;
using MenuOptics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [SubscriptionRequired]
    [Authorize(Roles = "Admin,Restaurant")]
    public class CategoryApiController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        public CategoryApiController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        private int ActualUserId => int.Parse(User.FindFirst("ActualUserId")?.Value ?? "0");

        [HttpGet("GetAllCategories/{isActive?}")]
        public async Task<IActionResult> GetAllCategories(bool? isActive = null)
        {
            var responseData = await _categoryService.GetAllCategoriesAsync(CurrentUserId, isActive);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpGet("GetCategory/{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var responseData = await _categoryService.GetCategoryAsync(id, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPost("CreateCategory")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDTO model)
        {
            var responseData = await _categoryService.CreateCategoryAsync(model, CurrentUserId, ActualUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPut("UpdateCategory")]
        public async Task<IActionResult> UpdateCategory([FromBody] UpdateCategoryDTO model)
        {
            var responseData = await _categoryService.UpdateCategoryAsync(model, CurrentUserId, ActualUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPut("ToggleCategoryActive/{id}")]
        public async Task<IActionResult> ToggleCategoryActive(int id)
        {
            var responseData = await _categoryService.ToggleCategoryActiveAsync(id, CurrentUserId, ActualUserId);
            return StatusCode(responseData.Status, responseData);
        }

    }
}
