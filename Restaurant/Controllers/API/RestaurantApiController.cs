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
            var response = await _restaurantService.GetMyRestaurantsAsync(NameIdentifierClaim, RequestId);
            return StatusCode(response.Status, response);
        }

        [HttpGet("GetMyRestaurant/{id}")]
        public async Task<IActionResult> GetMyRestaurant(int id)
        {
            var response = await _restaurantService.GetMyRestaurantAsync(id, NameIdentifierClaim, RequestId);
            return StatusCode(response.Status, response);
        }

        [HttpPost("SignupRestaurant")]
        public async Task<IActionResult> SignupRestaurant([FromBody] CreateRestaurant model)
        {
            var requestId = RequestId;
            var payload = System.Text.Json.JsonSerializer.Serialize(model);
            var response = new Response();

            if (model.RestaurantId <= 0)
            {
                response.Status = 400;
                response.ResponseMsg = "A valid RestaurantId is required. RestaurantId cannot be null or 0.";
                response.Data = ModelState;
                await _activityLog.LogAsync(null, requestId, "POST /api/RestaurantApi/SignupRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(response), "Warning");
                return BadRequest(response);
            }
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .SelectMany(x => x.Value.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();

                response.Status = 400;
                response.ResponseMsg = string.Join(" ", errors);
                response.Data = null;

                await _activityLog.LogAsync(null, requestId, "POST /api/RestaurantApi/SignupRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(response), "Warning");
                return BadRequest(response);
            }

            var result = await _restaurantService.SignupRestaurantAsync(model, NameIdentifierClaim, requestId);
            return StatusCode(result.Status, result);
        }

        [HttpPut("UpdateRestaurant")]
        public async Task<IActionResult> UpdateRestaurant([FromBody] UpdateRestaurant model)
        {
            var response = await _restaurantService.UpdateRestaurantAsync(model, NameIdentifierClaim, RequestId);
            return StatusCode(response.Status, response);
        }

        [HttpPut("ToggleRestaurantActive/{restaurantId}")]
        public async Task<IActionResult> ToggleRestaurantActive(int restaurantId, [FromBody] RestaurantStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new Response
                {
                    Status = 400,
                    ResponseMsg = "isActive must be either true or false."
                });
            }

            var response = await _restaurantService.ToggleRestaurantActiveAsync(restaurantId, request, NameIdentifierClaim, RequestId);
            return StatusCode(response.Status, response);
        }

        [HttpPost("SetPassword")]
        public async Task<IActionResult> SetPassword(SetPasswordDTO model)
        {
            var response = await _restaurantService.SetPasswordAsync(model, NameIdentifierClaim);
            return StatusCode(response.Status, response);
        }
    }
}
