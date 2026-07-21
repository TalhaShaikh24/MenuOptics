using Microsoft.AspNetCore.Authentication.Cookies;
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
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class UserApiController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserApiController(IUserService userService)
        {
            _userService = userService;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        private int CurrentUserIdOrDefault => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1");

        private string RequestBaseUrl => $"{Request.Scheme}://{Request.Host}";

        private string RequestId => HttpContext.Items[RequestIdMiddleware.RequestIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();

        private Response ModelStateErrorResponse()
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .SelectMany(x => x.Value.Errors)
                .Select(x => x.ErrorMessage)
                .ToList();

            return new Response
            {
                Status = 400,
                ResponseMsg = string.Join(" ", errors)
            };
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("GetUserStats")]
        public async Task<IActionResult> GetUserStats()
        {
            var response = await _userService.GetUserStatsAsync();
            return StatusCode(response.Status, response);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("GetRestaurantStats")]
        public async Task<IActionResult> GetRestaurantStats()
        {
            var response = await _userService.GetRestaurantStatsAsync();
            return StatusCode(response.Status, response);
        }

        [AllowAnonymous]
        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser([FromForm] User model)
        {
            var response = await _userService.CreateUserAsync(model, CurrentUserIdOrDefault, RequestBaseUrl);
            return StatusCode(response.Status, response);
        }

        [HttpGet("GetUser/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var response = await _userService.GetUserAsync(id);
            return StatusCode(response.Status, response);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            var response = await _userService.GetAllUsersAsync();
            return StatusCode(response.Status, response);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllRestaurants")]
        public async Task<IActionResult> GetAllRestaurants()
        {
            var response = await _userService.GetAllRestaurantsAsync();
            return StatusCode(response.Status, response);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("UpdateUser")]
        public async Task<IActionResult> UpdateUser([FromForm] User model)
        {
            var response = await _userService.UpdateUserAsync(model, CurrentUserIdOrDefault, RequestBaseUrl);
            return StatusCode(response.Status, response);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("ToggleUserActive/{id}")]
        public async Task<IActionResult> ToggleUserActive(int id)
        {
            var response = await _userService.ToggleUserActiveAsync(id, CurrentUserIdOrDefault);
            return StatusCode(response.Status, response);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("DeleteUser/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var response = await _userService.DeleteUserAsync(id);
            return StatusCode(response.Status, response);
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var response = await _userService.LoginAsync(model, RequestId);
            return StatusCode(response.Status, response);
        }

        [AllowAnonymous]
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            var response = await _userService.LogoutAsync();
            return StatusCode(response.Status, response);
        }

        [HttpPost("AddRestaurantMember")]
        [Authorize(Roles = "Restaurant,Admin")]
        public async Task<IActionResult> AddRestaurantMember([FromBody] AddRestaurantMemberDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelStateErrorResponse());
            }

            var callerRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var isParentClaim = User.FindFirst("IsParent")?.Value ?? "true";

            var response = await _userService.AddRestaurantMemberAsync(model, CurrentUserId, callerRole, isParentClaim);
            return StatusCode(response.Status, response);
        }

        [HttpGet("GetRestaurantMembers")]
        [Authorize(Roles = "Restaurant,Admin")]
        public async Task<IActionResult> GetRestaurantMembers()
        {
            var response = await _userService.GetRestaurantMembersAsync(CurrentUserId);
            return StatusCode(response.Status, response);
        }

        [HttpGet("GetMemberStats")]
        [Authorize(Roles = "Restaurant,Admin")]
        public async Task<IActionResult> GetMemberStats()
        {
            var response = await _userService.GetMemberStatsAsync(CurrentUserId);
            return StatusCode(response.Status, response);
        }

        [HttpPut("UpdateRestaurantMember")]
        [Authorize(Roles = "Restaurant,Admin")]
        public async Task<IActionResult> UpdateRestaurantMember([FromForm] UpdateRestaurantMemberDTO model)
        {
            var response = await _userService.UpdateRestaurantMemberAsync(model, CurrentUserId, RequestBaseUrl);
            return StatusCode(response.Status, response);
        }

        [HttpPut("ToggleMemberActive/{id}")]
        [Authorize(Roles = "Restaurant,Admin")]
        public async Task<IActionResult> ToggleMemberActive(int id)
        {
            var response = await _userService.ToggleMemberActiveAsync(id, CurrentUserId);
            return StatusCode(response.Status, response);
        }

        [HttpDelete("DeleteMember/{id}")]
        [Authorize(Roles = "Restaurant,Admin")]
        public async Task<IActionResult> DeleteMember(int id)
        {
            var response = await _userService.DeleteMemberAsync(id, CurrentUserId);
            return StatusCode(response.Status, response);
        }

        [AllowAnonymous]
        [HttpPost("CreateCheckoutSession")]
        public IActionResult CreateCheckoutSession([FromBody] PlanRequest request)
        {
            var sessionId = _userService.CreateCheckoutSession(request.Plan, request.UserId);
            return Ok(new { sessionId });
        }

        [AllowAnonymous]
        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest model)
        {
            var response = await _userService.RefreshTokenAsync(model);
            return StatusCode(response.Status, response);
        }

        [AllowAnonymous]
        [HttpPost("RevokeToken")]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest model)
        {
            var response = await _userService.RevokeTokenAsync(model);
            return StatusCode(response.Status, response);
        }

        [AllowAnonymous]
        [HttpPost("CheckEmail")]
        public async Task<IActionResult> CheckEmail([FromBody] CheckEmailModel model)
        {
            var response = await _userService.CheckEmailAsync(model);
            return StatusCode(response.Status, response);
        }

        [AllowAnonymous]
        [HttpPost("CompleteRegistration")]
        public async Task<IActionResult> CompleteRegistration([FromForm] CompleteRegistrationDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelStateErrorResponse());
            }

            var response = await _userService.CompleteRegistrationAsync(model, RequestBaseUrl);
            return StatusCode(response.Status, response);
        }

        [AllowAnonymous]
        [HttpPost("SendOtp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelStateErrorResponse());
            }

            var response = await _userService.SendOtpAsync(model);
            return StatusCode(response.Status, response);
        }

        [AllowAnonymous]
        [HttpPost("VerifyOtp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelStateErrorResponse());
            }

            var response = await _userService.VerifyOtpAsync(model);
            return StatusCode(response.Status, response);
        }

        public class PlanRequest
        {
            public string? Plan { get; set; }
            public int UserId { get; set; }
        }
    }
}
