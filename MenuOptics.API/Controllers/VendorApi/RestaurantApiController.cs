using MenuOptics.API.Middleware;
using MenuOptics.Middleware;
using MenuOptics.Models;
using MenuOptics.Services;
using MenuOptics.Services.EmailTemplates;
using MenuOptics.Services.VendorApi;
using MenuOptics.Services.VendorApiServices.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Security.Claims;

namespace MenuOptics.API.Controllers.VendorApi
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "Vendor")]
    public class RestaurantApiController : ControllerBase
    {
        private readonly IRestaurantService _restaurantService;
        private readonly IVendorActivityLogService _vendorActivityLog;
        private readonly ISmtpEmailService _emailService;
        private readonly IConfiguration _configuration;

        public RestaurantApiController(IRestaurantService restaurantService, IVendorActivityLogService activityLog, ISmtpEmailService emailService, IConfiguration configuration)
        {
            _restaurantService = restaurantService;
            _vendorActivityLog = activityLog;
            _emailService = emailService;
            _configuration = configuration;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        private string RequestId = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant();

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
                await _vendorActivityLog.LogAsync(CurrentUserId, requestId, "POST /api/RestaurantApi/SignupRestaurant",
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

                await _vendorActivityLog.LogAsync(CurrentUserId, requestId, "POST /api/RestaurantApi/SignupRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(responseData), "Warning");
                return BadRequest(responseData);
            }

            var result = await _restaurantService.SignupRestaurantAsync(model, CurrentUserId, requestId);

            if (result.Status is 200 or 201)
            {
                var webUrl = _configuration["WebUrl"] ?? "https://menuoptics.ca";
                var htmlBody = WelcomeEmailTemplate.Build(model.FirstName, model.RestaurantName, webUrl, model.Email);
                await _emailService.SendEmailAsync(
                    model.Email,
                    "Welcome to Menu Optics!",
                    htmlBody);
            }

            return StatusCode(result.Status, result);
        }

        [HttpPut("UpdateRestaurant")]
        public async Task<IActionResult> UpdateRestaurant([FromBody] UpdateRestaurantDto model)
        {
            var responseData = await _restaurantService.UpdateRestaurantAsync(model, CurrentUserId, RequestId);
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

            var responseData = await _restaurantService.ToggleRestaurantActiveAsync(restaurantId, request, CurrentUserId, RequestId);
            return StatusCode(responseData.Status, responseData);
        }
    }
}
