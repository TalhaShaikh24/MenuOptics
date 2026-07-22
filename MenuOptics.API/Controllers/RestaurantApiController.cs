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
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "Vendor")]
    public class RestaurantApiController : ControllerBase
    {
        private readonly IRestaurantService _restaurantService;
        private readonly IActivityLogService _activityLog;

        public RestaurantApiController(IRestaurantService restaurantService, IActivityLogService activityLog)
        {
            _restaurantService = restaurantService;
            _activityLog = activityLog;
        }

        private string? NameIdentifierClaim => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        private string RequestId => HttpContext.Items[RequestIdMiddleware.RequestIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();

        [HttpGet("GetMyRestaurants")]
        public async Task<IActionResult> GetMyRestaurants()
        {
            var responseData = await _restaurantService.GetMyRestaurantsAsync(NameIdentifierClaim, RequestId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpGet("GetMyRestaurant/{id}")]
        public async Task<IActionResult> GetMyRestaurant(int id)
        {
            var responseData = await _restaurantService.GetMyRestaurantAsync(id, NameIdentifierClaim, RequestId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPost("SignupRestaurant")]
        public async Task<IActionResult> SignupRestaurant([FromBody] CreateRestaurantDto model)
        {
            var requestId = RequestId;
            var payload = System.Text.Json.JsonSerializer.Serialize(model);
            var responseData = new ResponseDto();

            if (model.RestaurantId <= 0)
            {
                responseData.Status = 400;
                responseData.ResponseMsg = "A valid RestaurantId is required. RestaurantId cannot be null or 0.";
                responseData.Data = ModelState;
                await _activityLog.LogAsync(null, requestId, "POST /api/RestaurantApi/SignupRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(responseData), "Warning");
                return BadRequest(responseData);
            }
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .SelectMany(x => x.Value.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();

                responseData.Status = 400;
                responseData.ResponseMsg = string.Join(" ", errors);
                responseData.Data = null;

                await _activityLog.LogAsync(null, requestId, "POST /api/RestaurantApi/SignupRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(responseData), "Warning");
                return BadRequest(responseData);
            }

            var result = await _restaurantService.SignupRestaurantAsync(model, NameIdentifierClaim, requestId);
            return StatusCode(result.Status, result);
        }

        [HttpPut("UpdateRestaurantDto")]
        public async Task<IActionResult> UpdateRestaurantDto([FromBody] UpdateRestaurantDto model)
        {
            var responseData = await _restaurantService.UpdateRestaurantAsync(model, NameIdentifierClaim, RequestId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPut("ToggleRestaurantActive/{restaurantId}")]
        public async Task<IActionResult> ToggleRestaurantActive(int restaurantId, [FromBody] RestaurantStatusRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDto
                {
                    Status = 400,
                    ResponseMsg = "isActive must be either true or false."
                });
            }

            var responseData = await _restaurantService.ToggleRestaurantActiveAsync(restaurantId, request, NameIdentifierClaim, RequestId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPost("SetPassword")]
        public async Task<IActionResult> SetPassword(SetPasswordDto model)
        {
            var responseData = await _restaurantService.SetPasswordAsync(model, NameIdentifierClaim);
            return StatusCode(responseData.Status, responseData);
        }
    }
}
