using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Models;
using MenuOptics.Services;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class ProfileApiController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileApiController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet("GetCurrentUser")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var (statusCode, body) = await _profileService.GetCurrentUserAsync(userId);
            return StatusCode(statusCode, body);
        }

        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromForm] User model)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var (statusCode, body) = await _profileService.UpdateProfileAsync(model, currentUserId, $"{Request.Scheme}://{Request.Host}");
            return StatusCode(statusCode, body);
        }
    }
}
