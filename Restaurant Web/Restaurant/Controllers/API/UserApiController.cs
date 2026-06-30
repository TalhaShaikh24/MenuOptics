using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Data;
using Restaurant.Models;
using Stripe;
using Stripe.Checkout;
using System.Data;
using System.Security.Claims;

namespace Restaurant.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class UserApiController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly IWebHostEnvironment _environment;

        public UserApiController(DapperContext context, IWebHostEnvironment environment, IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("GetRestaurantStats")]
        public async Task<IActionResult> GetRestaurantStats()
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_RESTAURANT_STATS");

                var stats = await connection.QueryFirstOrDefaultAsync<dynamic>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (stats == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "No restaurant stats available.";
                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "Restaurant stats retrieved successfully.";
                response.Data = new
                {
                    TotalRestaurants = stats.TotalRestaurants,
                    ActiveToday = stats.ActiveToday,
                    InactiveRestaurants = stats.InactiveRestaurants,
                    NewThisWeek = stats.NewThisWeek
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        [AllowAnonymous]
        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser([FromForm] User model)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var FilePathUrl = "";
                if (model.File != null)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath ?? Directory.GetCurrentDirectory(), "Uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.File.FileName);
                    string filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.File.CopyToAsync(stream);
                    }
                    FilePathUrl = $"{Request.Scheme}://{Request.Host}/Uploads/{fileName}";
                }

                var parameters = new DynamicParameters();
                parameters.Add("@FirstName", model.FirstName);
                parameters.Add("@LastName", model.LastName);
                parameters.Add("@Username", model.Username);
                parameters.Add("@Email", model.Email);
                parameters.Add("@Phone", model.Phone);
                parameters.Add("@ProfilePic", FilePathUrl);
                parameters.Add("@Address", model.Address);
                parameters.Add("@Country", model.Country);
                parameters.Add("@State", model.State);
                parameters.Add("@City", model.City);
                parameters.Add("@Zip", model.Zip);
                parameters.Add("@Gender", model.Gender);
                parameters.Add("@RestaurantName", model.Role == "restaurant" || model.Role == "Restaurant" || model.Role == "admin" ? model.RestaurantName : null);
                parameters.Add("@CuisineType", model.Role == "restaurant" || model.Role == "Restaurant" || model.Role == "admin" ? model.CuisineType : null);
                parameters.Add("@RestaurantSize", model.Role == "restaurant" || model.Role == "Restaurant" || model.Role == "admin" ? model.RestaurantSize : null);
                parameters.Add("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(model.Password));
                parameters.Add("@RoleName", model.Role);
                parameters.Add("@IsActive", model.IsActive);
                parameters.Add("@SubscriptionStatus", "Pending"); // Set to Pending
                parameters.Add("@CreatedBy", model.CreatedBy ?? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1"));
                parameters.Add("@Action", "INSERT");

                int userId = await connection.ExecuteScalarAsync<int>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                response.Status = 200;
                response.ResponseMsg = "User created successfully.";
                response.Data = new { UserId = userId };
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        [HttpGet("GetUser/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", id);
                parameters.Add("@Action", "SELECT");

                var userProfile = await connection.QueryFirstOrDefaultAsync<UserDTO>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (userProfile == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "User profile not found.";
                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "User profile retrieved successfully.";
                response.Data = userProfile;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL");

                var users = await connection.QueryAsync<UserDTO>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                response.Status = 200;
                response.ResponseMsg = "Users retrieved successfully.";
                response.Data = users;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllRestaurants")]
        public async Task<IActionResult> GetAllRestaurants()
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL_RESTAURANT");

                var users = await connection.QueryAsync<UserDTO>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                response.Status = 200;
                response.ResponseMsg = "Users retrieved successfully.";
                response.Data = users;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("UpdateUser")]
        public async Task<IActionResult> UpdateUser([FromForm] User model)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (model.UserId <= 0)
                {
                    response.Status = 400;
                    response.ResponseMsg = "UserId is required for updating a user.";
                    return BadRequest(response);
                }

                var FilePathUrl = model.ProfilePic;
                if (model.File != null)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath ?? Directory.GetCurrentDirectory(), "Uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.File.FileName);
                    string filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.File.CopyToAsync(stream);
                    }
                    FilePathUrl = $"{Request.Scheme}://{Request.Host}/Uploads/{fileName}";
                }

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", model.UserId);
                parameters.Add("@FirstName", model.FirstName);
                parameters.Add("@LastName", model.LastName);
                parameters.Add("@Username", model.Username);
                parameters.Add("@Email", model.Email);
                parameters.Add("@Phone", model.Phone);
                parameters.Add("@ProfilePic", FilePathUrl);
                parameters.Add("@Address", model.Address);
                parameters.Add("@Country", model.Country);
                parameters.Add("@State", model.State);
                parameters.Add("@City", model.City);
                parameters.Add("@Zip", model.Zip);
                parameters.Add("@Gender", model.Gender);
                parameters.Add("@RestaurantName", model.Role == "Restaurant" || model.Role == "Customer" ? model.RestaurantName : null);
                parameters.Add("@CuisineType", model.Role == "Restaurant" || model.Role == "Customer" ? model.CuisineType : null);
                parameters.Add("@RestaurantSize", model.Role == "Restaurant" || model.Role == "Customer" ? model.RestaurantSize : null);
                parameters.Add("@RoleName", model.Role);
                parameters.Add("@IsActive", model.IsActive);
                parameters.Add("@ModifiedBy", int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1"));
                parameters.Add("@Action", "UPDATE");

                int userId = await connection.ExecuteScalarAsync<int>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                response.Status = 200;
                response.ResponseMsg = "User updated successfully.";
                response.Data = new { UserId = userId };
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("ToggleUserActive/{id}")]
        public async Task<IActionResult> ToggleUserActive(int id)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1");
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", id);
                parameters.Add("@Action", "SELECT");
                var user = await connection.QueryFirstOrDefaultAsync<UserDTO>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (user == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "User not found.";
                    return NotFound(response);
                }

                parameters = new DynamicParameters();
                parameters.Add("@UserId", id);
                parameters.Add("@IsActive", !user.IsActive);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "TOGGLE_ACTIVE");

                await connection.ExecuteAsync("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                response.Status = 200;
                response.ResponseMsg = $"User {(user.IsActive ? "deactivated" : "activated")} successfully.";
                response.Data = new { UserId = id, IsActive = !user.IsActive };
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("DeleteUser/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", id);
                parameters.Add("@Action", "DELETE");

                var affectedRows = await connection.ExecuteAsync("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                if (affectedRows == 0)
                {
                    response.Status = 404;
                    response.ResponseMsg = "User not found.";
                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "User deleted successfully.";
                response.Data = new { UserId = id };
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Username", model.Username);
                parameters.Add("@Action", "LOGIN");

                var user = await connection.QueryFirstOrDefaultAsync<UserDTO>(
                    "sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    response.Status = 401;
                    response.ResponseMsg = "Your subscription is not active yet. Please wait for the admin to activate your account";
                    return Unauthorized(response);
                }


               


               

                var claims = new List<Claim>
{
    new Claim(ClaimTypes.Name, user.Username),
    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
    new Claim(ClaimTypes.Role, user.Roles),
    new Claim("FirstName", user.FirstName ?? ""),
    new Claim("LastName", user.LastName ?? "")
};


                // Create claims identity
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Create authentication properties
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                };


                // Sign in the user with cookies
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                response.Status = 200;
                response.ResponseMsg = "Login successful.";
                response.Data = new { UserId = user.UserId, Roles = user.Roles };

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        [AllowAnonymous]
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            var response = new Response();
            // Sign out the user by clearing the authentication cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            response.Status = 200;
            response.ResponseMsg = "Logout successful.";
            response.Data = null;
            return Ok(response);
        }


        [AllowAnonymous]
        [HttpPost("CreateCheckoutSession")]
        public IActionResult CreateCheckoutSession([FromBody] PlanRequest request)
        {
            StripeConfiguration.ApiKey = "sk_test_51M9O4qKR3yBF1l8fZJ5Ll6Q7x0V0ec9i8rgqi8FMKtHTqSx3WYMvdCeOpuOdpImJ6OmhTiq6DDUhB83zumFikT2700QEmhSeSP";

      

            var priceId = request.Plan switch
            {
                "starter" => "price_1Rt2KhKR3yBF1l8fWC1Q2J6T",
                "professional" => "price_1Rt2LNKR3yBF1l8f4ww3GlNh",
                _ => throw new Exception("Invalid plan")
            };

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "subscription",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1,
                    },
                },
                        Metadata = new Dictionary<string, string>
                {
                    { "UserId", request.UserId.ToString() } // Pass UserId from the client
                },
                SuccessUrl = $"https://localhost:7103/Payment/successpayment?session_id={{CHECKOUT_SESSION_ID}}&user_id={request.UserId}",
                CancelUrl = "https://localhost:7103/Payment/Cancel",
            };

            var service = new SessionService();
            var session = service.Create(options);

            return Ok(new { sessionId = session.Id });
        }


        public class PlanRequest
        {
            public string? Plan { get; set; }
            public int UserId { get; set; }
        }

    }
}