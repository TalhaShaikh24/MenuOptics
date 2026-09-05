using MenuOptics.API.Middleware;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [SubscriptionRequired]
    [ApiController]
    public class MenuApiController : ControllerBase
    {
        private readonly IMenuService _menuService;

        public MenuApiController(IMenuService menuService)
        {
            _menuService = menuService;
        }
        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        private int ActualUserId => int.Parse(User.FindFirst("ActualUserId")?.Value ?? "0");

        [HttpPost("CreateMenuItem")]
        public async Task<IActionResult> CreateMenuItem([FromForm] Menu model, [FromForm] string ingredients, [FromForm] string recipes)
        {
            var responseData = await _menuService.CreateMenuItemAsync(model, ingredients, recipes, CurrentUserId, ActualUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPut("UpdateMenuItem")]
        public async Task<IActionResult> UpdateMenuItem([FromForm] Menu model, [FromForm] string ingredients, [FromForm] string recipes)
        {
            var responseData = await _menuService.UpdateMenuItemAsync(model, ingredients, recipes, CurrentUserId, ActualUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpGet("GetMenuItem/{id}")]
        public async Task<IActionResult> GetMenuItem(int id)
        {
            var responseData = await _menuService.GetMenuItemAsync(id);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpGet("GetAllMenuItems")]
        public async Task<IActionResult> GetAllMenuItems(bool? isActive)
        {
            var responseData = await _menuService.GetAllMenuItemsAsync(isActive, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPut("ToggleMenuActive/{id}")]
        public async Task<IActionResult> ToggleMenuActive(int id)
        {
            var responseData = await _menuService.ToggleMenuActiveAsync(id, CurrentUserId, ActualUserId);
            return StatusCode(responseData.Status, responseData);
        }
    }
}
