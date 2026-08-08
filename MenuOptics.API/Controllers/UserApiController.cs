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
        private readonly IAuthService _authService;

        public UserApiController(IUserService userService, IAuthService authService)
        {
            _userService = userService;
            _authService = authService;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        private int CurrentUserIdOrDefault => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1");

        private string RequestBaseUrl => $"{Request.Scheme}://{Request.Host}";

        private string RequestId => HttpContext.Items[RequestIdMiddleware.RequestIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();

        private ResponseDto ModelStateErrorResponse()
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .SelectMany(x => x.Value.Errors)
                .Select(x => x.ErrorMessage)
                .ToList();

            return new ResponseDto
            {
                Status = 400,
                ResponseMsg = string.Join(" ", errors)
            };
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("GetUserStats")]
        public async Task<IActionResult> GetUserStats()
        {
            var responseData = await _userService.GetUserStatsAsync();
            return StatusCode(responseData.Status, responseData);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("GetRestaurantStats")]
        public async Task<IActionResult> GetRestaurantStats()
        {
            var responseData = await _userService.GetRestaurantStatsAsync();
            return StatusCode(responseData.Status, responseData);
        }

        [AllowAnonymous]
        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser([FromForm] User model)
        {
            var responseData = await _userService.CreateUserAsync(model, CurrentUserIdOrDefault, RequestBaseUrl);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpGet("GetUser/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var responseData = await _userService.GetUserAsync(id);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpGet("GetCurrentUser")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var responseData = await _userService.GetUserAsync(userId);
            return StatusCode(responseData.Status, responseData);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            var responseData = await _userService.GetAllUsersAsync();
            return StatusCode(responseData.Status, responseData);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllRestaurants")]
        public async Task<IActionResult> GetAllRestaurants()
        {
            var responseData = await _userService.GetAllRestaurantsAsync();
            return StatusCode(responseData.Status, responseData);
        }

        [HttpGet("GetAllVendors")]
        public async Task<IActionResult> GetAllVendors()
        {
            var responseData = await _userService.GetAllVendorsAsync(CurrentUserIdOrDefault);
            return StatusCode(responseData.Status, responseData);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("UpdateUser")]
        public async Task<IActionResult> UpdateUser([FromForm] User model)
        {
            var responseData = await _userService.UpdateUserAsync(model, CurrentUserIdOrDefault, RequestBaseUrl);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPut("UpdateCurrentUser")]
        public async Task<IActionResult> UpdateCurrentUser([FromForm] User model)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            model.UserId = currentUserId;
            var responseData = await _userService.UpdateUserAsync(model, currentUserId, $"{Request.Scheme}://{Request.Host}");
            return StatusCode(responseData.Status, responseData);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("ToggleUserActive/{id}")]
        public async Task<IActionResult> ToggleUserActive(int id)
        {
            var responseData = await _userService.ToggleUserActiveAsync(id, CurrentUserIdOrDefault);
            return StatusCode(responseData.Status, responseData);
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var responseData = await _authService.LoginAsync(model, RequestId);
            return StatusCode(responseData.Status, responseData);
        }

        [AllowAnonymous]
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            var responseData = await _authService.LogoutAsync();
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPost("AddRestaurantMember")]
        [Authorize(Roles = "Restaurant,Admin")]
        public async Task<IActionResult> AddRestaurantMember([FromBody] AddRestaurantMemberDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelStateErrorResponse());
            }

            var callerRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var isParentClaim = User.FindFirst("IsParent")?.Value ?? "true";

            var responseData = await _userService.AddRestaurantMemberAsync(model, CurrentUserId, callerRole, isParentClaim);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpGet("GetRestaurantMembers")]
        [Authorize(Roles = "Restaurant,Admin")]
        public async Task<IActionResult> GetRestaurantMembers()
        {
            var responseData = await _userService.GetRestaurantMembersAsync(CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpGet("GetMemberStats")]
        [Authorize(Roles = "Restaurant,Admin")]
        public async Task<IActionResult> GetMemberStats()
        {
            var responseData = await _userService.GetMemberStatsAsync(CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPut("UpdateRestaurantMember")]
        [Authorize(Roles = "Restaurant,Admin")]
        public async Task<IActionResult> UpdateRestaurantMember([FromForm] UpdateRestaurantMemberDto model)
        {
            var responseData = await _userService.UpdateRestaurantMemberAsync(model, CurrentUserId, RequestBaseUrl);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPut("ToggleMemberActive/{id}")]
        [Authorize(Roles = "Restaurant,Admin")]
        public async Task<IActionResult> ToggleMemberActive(int id)
        {
            var responseData = await _userService.ToggleMemberActiveAsync(id, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [AllowAnonymous]
        [HttpPost("CreateCheckoutSession")]
        public IActionResult CreateCheckoutSession([FromBody] PlanRequest request)
        {
            var sessionId = _authService.CreateCheckoutSession(request.Plan, request.UserId);
            return Ok(new { sessionId });
        }

        [AllowAnonymous]
        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto model)
        {
            var responseData = await _authService.RefreshTokenAsync(model);
            return StatusCode(responseData.Status, responseData);
        }

        [AllowAnonymous]
        [HttpPost("RevokeToken")]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequestDto model)
        {
            var responseData = await _authService.RevokeTokenAsync(model);
            return StatusCode(responseData.Status, responseData);
        }

        [AllowAnonymous]
        [HttpPost("CheckEmail")]
        public async Task<IActionResult> CheckEmail([FromBody] CheckEmailDto model)
        {
            var responseData = await _authService.CheckEmailAsync(model);
            return StatusCode(responseData.Status, responseData);
        }

        [AllowAnonymous]
        [HttpPost("CompleteRegistration")]
        public async Task<IActionResult> CompleteRegistration([FromForm] CompleteRegistrationDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelStateErrorResponse());
            }

            var responseData = await _userService.CompleteRegistrationAsync(model, RequestBaseUrl);
            return StatusCode(responseData.Status, responseData);
        }

        [AllowAnonymous]
        [HttpPost("SendOtp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelStateErrorResponse());
            }

            var responseData = await _authService.SendOtpAsync(model);
            return StatusCode(responseData.Status, responseData);
        }

        [AllowAnonymous]
        [HttpPost("VerifyOtp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelStateErrorResponse());
            }

            var responseData = await _authService.VerifyOtpAsync(model);
            return StatusCode(responseData.Status, responseData);
        }

        public class PlanRequest
        {
            public string? Plan { get; set; }
            public int UserId { get; set; }
        }
    }
}
