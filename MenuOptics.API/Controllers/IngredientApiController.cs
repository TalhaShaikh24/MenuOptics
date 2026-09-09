using MenuOptics.API.Middleware;
using MenuOptics.Middleware;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [SubscriptionRequired]
    [ApiController]
    public class IngredientApiController : ControllerBase
    {
        private readonly IIngredientService _ingredientService;
        private readonly IActivityLogService _activityLog;
        public IngredientApiController(IIngredientService ingredientService, IActivityLogService activityLog)
        {
            _ingredientService = ingredientService;
            _activityLog = activityLog;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        private int ActualUserId => int.Parse(User.FindFirst("ActualUserId")?.Value ?? "0");

        private string RequestId => HttpContext.Items[RequestIdMiddleware.RequestIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();

        [HttpPost("CreateIngredient")]
        [HttpPost]
        public async Task<IActionResult> CreateIngredient([FromBody] Ingredient model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDto
                {
                    Status = 400,
                    ResponseMsg = "Invalid input.",
                    Data = ModelState
                });
            }

            var (statusCode, body) = await _ingredientService.CreateIngredientAsync(model, CurrentUserId, ActualUserId);
            return StatusCode(statusCode, body);
        }

        [HttpPut("UpdateIngredient")]
        public async Task<IActionResult> UpdateIngredient([FromBody] Ingredient model)
        {
            var (statusCode, body) = await _ingredientService.UpdateIngredientAsync(model, CurrentUserId, ActualUserId);
            return StatusCode(statusCode, body);
        }
        
        [HttpGet("GetIngredient/{id}")]
        public async Task<IActionResult> GetIngredient(int id)
        {
            var (statusCode, body) = await _ingredientService.GetIngredientAsync(id);
            return StatusCode(statusCode, body);
        }

        [HttpGet("GetAllActiveIngredientsByUserId")]
        public async Task<IActionResult> GetAllActiveIngredientsByUserId()
        {
            var (statusCode, body) = await _ingredientService.GetAllActiveIngredientsByUserIdAsync(Request.Query, CurrentUserId);
            return StatusCode(statusCode, body);
        }

        [HttpGet("GetAllActiveInactiveIngredients")]
        public async Task<IActionResult> GetAllActiveInactiveIngredients()
        {
            var (statusCode, body) = await _ingredientService.GetAllActiveInactiveIngredientsAsync(Request.Query, CurrentUserId);
            return StatusCode(statusCode, body);
        }

        [HttpGet("GetAllIngredientsByVendorId/{vendorId}/{isPrivate}")]
        public async Task<IActionResult> GetAllIngredientsByVendorId(int vendorId, bool isPrivate)
        {
            int effectiveVendorId = vendorId;
            var (statusCode, body) = await _ingredientService.GetAllIngredientsByVendorIdAsync(Request.Query, effectiveVendorId, CurrentUserId, isPrivate);
            return StatusCode(statusCode, body);
        }

        [HttpGet("GetAllIngredientsByUserId/{userId}")]
        public async Task<IActionResult> GetAllIngredientsByUserId(int userId)
        {
            var (statusCode, body) = await _ingredientService.GetAllIngredientsByUserIdParamAsync(userId, Request.Query);
            return StatusCode(statusCode, body);
        }

        [HttpPut("ToggleIngredientActive/{id}")]
        public async Task<IActionResult> ToggleIngredientActive(int id)
        {
            var (statusCode, body) = await _ingredientService.ToggleIngredientActiveAsync(id, CurrentUserId, ActualUserId);
            return StatusCode(statusCode, body);
        }

    }
}
