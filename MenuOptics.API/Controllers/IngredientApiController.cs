using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Middleware;
using MenuOptics.Models;
using MenuOptics.Services;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
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

        private int CurrentUserIdOrDefault => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1");

        private string RequestId => HttpContext.Items[RequestIdMiddleware.RequestIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Authorize(Roles = "Vendor")]
        [HttpPost("CreateIngredientByVendor")]
        public async Task<IActionResult> CreateIngredientByVendor([FromBody] VendorIngredientDto model)
        {
            if (!ModelState.IsValid)
            {
                var responseData = new ResponseDto
                {
                    Status = 400,
                    ResponseMsg = "Invalid input."
                };
                await _activityLog.LogAsync(CurrentUserId, RequestId, "POST /api/IngredientApi/CreateIngredientByVendor",
                    System.Text.Json.JsonSerializer.Serialize(model), System.Text.Json.JsonSerializer.Serialize(responseData), "Warning");
                return BadRequest(responseData);
            }

            var (statusCode, body) = await _ingredientService.CreateIngredientByVendorAsync(model, CurrentUserId, RequestId);
            return StatusCode(statusCode, body);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Authorize(Roles = "Vendor")]
        [HttpPut("UpdateIngredientPrices")]
        public async Task<IActionResult> UpdateIngredientPrices([FromBody] UpdateIngredientPriceDto model)
        {
            var (statusCode, body) = await _ingredientService.UpdateIngredientPricesAsync(model, CurrentUserId, RequestId);
            return StatusCode(statusCode, body);
        }

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

            var (statusCode, body) = await _ingredientService.CreateIngredientAsync(model, CurrentUserId);
            return StatusCode(statusCode, body);
        }

        [HttpPost("AddIngredientStock")]
        public async Task<IActionResult> AddIngredientStock([FromBody] Inventory model)
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

            var (statusCode, body) = await _ingredientService.AddIngredientStockAsync(model, CurrentUserId);
            return StatusCode(statusCode, body);
        }

        [HttpGet("GetIngredient/{id}")]
        public async Task<IActionResult> GetIngredient(int id)
        {
            var (statusCode, body) = await _ingredientService.GetIngredientAsync(id);
            return StatusCode(statusCode, body);
        }

        [HttpGet("GetAllIngredients")]
        public async Task<IActionResult> GetAllIngredients()
        {
            var (statusCode, body) = await _ingredientService.GetAllIngredientsAsync(Request.Query);
            return StatusCode(statusCode, body);
        }

        [HttpGet("GetAllActiveIngredientsByUserId")]
        public async Task<IActionResult> GetAllActiveIngredientsByUserId()
        {
            var (statusCode, body) = await _ingredientService.GetAllActiveIngredientsByUserIdAsync(Request.Query, CurrentUserIdOrDefault);
            return StatusCode(statusCode, body);
        }

        [HttpGet("GetAllActiveInactiveIngredients")]
        public async Task<IActionResult> GetAllActiveInactiveIngredients()
        {
            var (statusCode, body) = await _ingredientService.GetAllActiveInactiveIngredientsAsync(Request.Query, CurrentUserIdOrDefault);
            return StatusCode(statusCode, body);
        }

        [HttpGet("GetAllIngredientsByVendorId/{vendorId}")]
        public async Task<IActionResult> GetAllIngredientsByVendorId(int vendorId)
        {
            int effectiveVendorId = vendorId;
            var (statusCode, body) = await _ingredientService.GetAllIngredientsByVendorIdAsync(Request.Query, effectiveVendorId, CurrentUserId);
            return StatusCode(statusCode, body);
        }

        [HttpGet("GetAllIngredientsByUserId/{userId}")]
        public async Task<IActionResult> GetAllIngredientsByUserId(int userId)
        {
            var (statusCode, body) = await _ingredientService.GetAllIngredientsByUserIdParamAsync(userId, Request.Query);
            return StatusCode(statusCode, body);
        }

        [HttpPut("UpdateIngredient")]
        public async Task<IActionResult> UpdateIngredient([FromBody] Ingredient model)
        {
            var (statusCode, body) = await _ingredientService.UpdateIngredientAsync(model, CurrentUserId);
            return StatusCode(statusCode, body);
        }

        // PATCH api/Ingredient/PatchIngredientDetails
        // Updates one or more of: Name, Unit, IsActive. Only provided (non-null) fields are applied.
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Authorize(Roles = "Vendor")]
        [HttpPatch("PatchIngredientDetails")]
        public async Task<IActionResult> PatchIngredientDetails([FromBody] UpdateIngredientDetailsDto model)
        {
            var (statusCode, body) = await _ingredientService.PatchIngredientDetailsAsync(model, CurrentUserId, RequestId);
            return StatusCode(statusCode, body);
        }


        [HttpPut("ToggleIngredientActive/{id}")]
        public async Task<IActionResult> ToggleIngredientActive(int id)
        {
            var (statusCode, body) = await _ingredientService.ToggleIngredientActiveAsync(id, CurrentUserId);
            return StatusCode(statusCode, body);
        }

    }
}
