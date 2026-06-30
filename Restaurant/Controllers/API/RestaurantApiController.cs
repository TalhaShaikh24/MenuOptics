using Dapper;
using MenuOptics.Data;
using MenuOptics.Middleware;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Roles = "Vendor")]
    public class RestaurantApiController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IActivityLogService _activityLog;

        public RestaurantApiController(DapperContext context, IWebHostEnvironment environment, IActivityLogService activityLog)
        {
            _context = context;
            _environment = environment;
            _activityLog = activityLog;
        }

        // ─────────────────────────────────────────────────────────────
        // Helper: Get VendorId from JWT claims 
        // ─────────────────────────────────────────────────────────────
        private int GetCallerVendorId()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(value, out int id))
                return id;

            throw new InvalidOperationException("Vendor identity could not be determined from token.");
        }
        
        // ─────────────────────────────────────────────────────────────
        // GET api/RestaurantApi/GetMyRestaurants
        // To get all restaurants of the logged-in vendor
        // ─────────────────────────────────────────────────────────────
        [HttpGet("GetMyRestaurants")]
        public async Task<IActionResult> GetMyRestaurants()
        {
            var requestId = HttpContext.Items[RequestIdMiddleware.RequestIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var callerVendorId = GetCallerVendorId();
                var parameters = new DynamicParameters();
                parameters.Add("@VendorId", callerVendorId);
                parameters.Add("@Action", "SELECT_ALL");

                var restaurants = await connection.QueryAsync<UserDTO>(
                    "sp_RestaurantCRUD", parameters,
                    commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Restaurants retrieved successfully.";
                response.Data = restaurants;

                await _activityLog.LogAsync(callerVendorId, requestId, "GET /api/RestaurantApi/GetMyRestaurants",
                    null, System.Text.Json.JsonSerializer.Serialize(response), "Success");

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";

                await _activityLog.LogAsync(null, requestId, "GET /api/RestaurantApi/GetMyRestaurants",
                    null, System.Text.Json.JsonSerializer.Serialize(response), "Error");

                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // GET api/RestaurantApi/GetMyRestaurant/{id}
        // Specific restaurant details for the logged-in vendor (only if it belongs to them)
        // ─────────────────────────────────────────────────────────────
        [HttpGet("GetMyRestaurant/{id}")]
        public async Task<IActionResult> GetMyRestaurant(int id)
        {
            var requestId = HttpContext.Items[RequestIdMiddleware.RequestIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var callerVendorId = GetCallerVendorId();
                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", id);
                parameters.Add("@VendorId", callerVendorId);
                parameters.Add("@Action", "SELECT");

                var restaurant = await connection.QueryFirstOrDefaultAsync<UserDTO>(
                    "sp_RestaurantCRUD", parameters,
                    commandType: CommandType.StoredProcedure);

                if (restaurant == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Restaurant not found or you do not have access to it.";

                    await _activityLog.LogAsync(callerVendorId, requestId, "GET /api/RestaurantApi/GetMyRestaurant",
                        id.ToString(), System.Text.Json.JsonSerializer.Serialize(response), "Warning");

                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "Restaurant retrieved successfully.";
                response.Data = restaurant;

                await _activityLog.LogAsync(callerVendorId, requestId, "GET /api/RestaurantApi/GetMyRestaurant",
                    id.ToString(), System.Text.Json.JsonSerializer.Serialize(response), "Success");

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";

                await _activityLog.LogAsync(null, requestId, "GET /api/RestaurantApi/GetMyRestaurant",
                    id.ToString(), System.Text.Json.JsonSerializer.Serialize(response), "Error");

                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // POST api/RestaurantApi/SignupRestaurant
        // Create new restaurant under the logged-in vendor (VendorId will be taken from token, ignore any passed value)
        // ─────────────────────────────────────────────────────────────
        [HttpPost("SignupRestaurant")]
        public async Task<IActionResult> SignupRestaurant([FromBody] CreateRestaurant model)
        {
            var requestId = HttpContext.Items[RequestIdMiddleware.RequestIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
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

            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var callerVendorId = GetCallerVendorId();

                if (callerVendorId == 0)
                {
                    response.Status = 401;
                    response.ResponseMsg = "Unauthorized";
                    await _activityLog.LogAsync(null, requestId, "POST /api/RestaurantApi/SignupRestaurant",
                        payload, System.Text.Json.JsonSerializer.Serialize(response), "Warning");
                    return Unauthorized(response);
                }



                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", model.RestaurantId);
                parameters.Add("@VendorId", callerVendorId);
                parameters.Add("@FirstName", model.FirstName);
                parameters.Add("@LastName", model.LastName);
                parameters.Add("@Username", model.Username);
                parameters.Add("@Email", model.Email);
                parameters.Add("@Phone", model.Phone);
                parameters.Add("@RestaurantName", model.RestaurantName);
                parameters.Add("@PasswordHash", BCrypt.Net.BCrypt.HashPassword("dbMF0sgx45FD0nVb"));
                parameters.Add("@SubscriptionStatus", "Pending");
                parameters.Add("@IsRegistrationCompleted", false);
                parameters.Add("@CreatedBy", callerVendorId);
                parameters.Add("@Action", "INSERT");

                int newId = await connection.ExecuteScalarAsync<int>(
                    "sp_RestaurantCRUD", parameters,
                    commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Restaurant signed up successfully.";
                response.Data = new { restaurantId = newId };

                await _activityLog.LogAsync(callerVendorId, requestId, "POST /api/RestaurantApi/SignupRestaurant",
                   payload, System.Text.Json.JsonSerializer.Serialize(new { response.Status, response.ResponseMsg, response.Data }), "Success");

                return Ok(response);
            }
            catch (SqlException sqlEx)
            {
                response.Status = 503;
                response.ResponseMsg = $"Exception: {sqlEx.Message}";

                await _activityLog.LogAsync(null, requestId, "POST /api/RestaurantApi/SignupRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(response), "Error");
                return StatusCode(503, response);
            }
            catch (InvalidOperationException invalidOpEx)
            {
                response.Status = 422;
                response.ResponseMsg = $"Invalid operation: {invalidOpEx.Message}";

                await _activityLog.LogAsync(null, requestId, "POST /api/RestaurantApi/SignupRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(response), "Error");
                return StatusCode(422, response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";

                await _activityLog.LogAsync(null, requestId, "POST /api/RestaurantApi/SignupRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(response), "Error");
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // PUT api/RestaurantApi/UpdateRestaurant
        // Update restaurant details
        // Check vendor ownership in controller (defense in depth) plus in SP (main check) 
        // ─────────────────────────────────────────────────────────────
        [HttpPut("UpdateRestaurant")]
        public async Task<IActionResult> UpdateRestaurant([FromBody] UpdateRestaurant model)
        {
            var requestId = HttpContext.Items[RequestIdMiddleware.RequestIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
            var payload = System.Text.Json.JsonSerializer.Serialize(model);
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (model.RestaurantId <= 0)
                {
                    response.Status = 400;
                    response.ResponseMsg = "RestaurantId is required for update.";

                    await _activityLog.LogAsync(null, requestId, "PUT /api/RestaurantApi/UpdateRestaurant",
                        payload, System.Text.Json.JsonSerializer.Serialize(response), "Warning");

                    return BadRequest(response);
                }

                var callerVendorId = GetCallerVendorId();

                var checkParams = new DynamicParameters();
                checkParams.Add("@RestaurantId", model.RestaurantId);
                checkParams.Add("@VendorId", callerVendorId);
                checkParams.Add("@Action", "SELECT");

                var existing = await connection.QueryFirstOrDefaultAsync<UserDTO>(
                    "sp_RestaurantCRUD", checkParams,
                    commandType: CommandType.StoredProcedure);

                if (existing == null)
                {
                    response.Status = 403;
                    response.ResponseMsg = "Access denied: You can only update your own restaurants.";

                    await _activityLog.LogAsync(callerVendorId, requestId, "PUT /api/RestaurantApi/UpdateRestaurant",
                        payload, System.Text.Json.JsonSerializer.Serialize(response), "Warning");

                    return StatusCode(403, response);
                }


                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", model.RestaurantId);
                parameters.Add("@VendorId", callerVendorId);
                parameters.Add("@FirstName", model.FirstName);
                parameters.Add("@LastName", model.LastName);
                parameters.Add("@Email", model.Email);
                parameters.Add("@Phone", model.Phone);
                parameters.Add("@ProfilePic", null);
                parameters.Add("@RestaurantName", model.RestaurantName);
                parameters.Add("@ModifiedBy", callerVendorId);
                parameters.Add("@Action", "UPDATE");

                var updated = await connection.QueryFirstOrDefaultAsync<UpdateRestaurant>(
                    "sp_RestaurantCRUD", parameters,
                    commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Restaurant updated successfully.";
                response.Data = updated;

                await _activityLog.LogAsync(callerVendorId, requestId, "PUT /api/RestaurantApi/UpdateRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(response), "Success");

                return Ok(response);
            }
            catch (SqlException sqlEx)
            {
                response.Status = 503;
                response.ResponseMsg = $"Exception: {sqlEx.Message}";

                await _activityLog.LogAsync(null, requestId, "PUT /api/RestaurantApi/UpdateRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(response), "Error");
                return StatusCode(503, response);
            }
            catch (InvalidOperationException invalidOpEx)
            {
                response.Status = 422;
                response.ResponseMsg = $"Invalid operation: {invalidOpEx.Message}";

                await _activityLog.LogAsync(null, requestId, "PUT /api/RestaurantApi/UpdateRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(response), "Error");
                return StatusCode(422, response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";

                await _activityLog.LogAsync(null, requestId, "PUT /api/RestaurantApi/UpdateRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(response), "Error");
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        // ─────────────────────────────────────────────────────────────
        
        [HttpPut("ToggleRestaurantActive/{restaurantId}")]
        public async Task<IActionResult> ToggleRestaurantActive(int restaurantId, [FromBody] RestaurantStatusRequest request)
        {
            var requestId = HttpContext.Items[RequestIdMiddleware.RequestIdItemKey]?.ToString()
                            ?? Guid.NewGuid().ToString();

            var payload = System.Text.Json.JsonSerializer.Serialize(request);

            var response = new Response();

            if (!ModelState.IsValid)
            {
                response.Status = 400;
                response.ResponseMsg = "isActive must be either true or false.";

                return BadRequest(response);
            }

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var callerVendorId = GetCallerVendorId();


                var checkParams = new DynamicParameters();
                checkParams.Add("@RestaurantId", restaurantId);
                checkParams.Add("@VendorId", callerVendorId);
                checkParams.Add("@Action", "SELECT");

                var existing = await connection.QueryFirstOrDefaultAsync<UserDTO>(
                    "sp_RestaurantCRUD", checkParams,
                    commandType: CommandType.StoredProcedure);

                if (existing == null)
                {
                    response.Status = 403;
                    response.ResponseMsg = "Access denied: You can only update your own restaurants.";

                    await _activityLog.LogAsync(callerVendorId, requestId, $"PUT /api/RestaurantApi/ToggleRestaurantActive/{restaurantId}",
                        payload, System.Text.Json.JsonSerializer.Serialize(response), "Warning");

                    return StatusCode(403, response);
                }



                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", restaurantId);
                parameters.Add("@VendorId", callerVendorId);
                parameters.Add("@IsActive", request.IsActive);
                parameters.Add("@ModifiedBy", callerVendorId);
                parameters.Add("@Action", "UPDATE_ACTIVE");

                await connection.ExecuteAsync(
                    "sp_RestaurantCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = $"Restaurant {(request.IsActive ? "activated" : "deactivated")} successfully.";
                response.Data = new
                {
                    RestaurantId = restaurantId,
                    IsActive = request.IsActive
                };

                return Ok(response);
            }
            catch (SqlException sqlEx)
            {
                response.Status = 503;
                response.ResponseMsg = $"Exception: {sqlEx.Message}";
                return StatusCode(503, response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
        }

        [HttpPost("SetPassword")]
        public async Task<IActionResult> SetPassword(SetPasswordDTO model)
        {
            var response = new Response();
            var callerVendorId = GetCallerVendorId();
            using var connection = _context.CreateConnection();

            try
            {
                connection.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", model.RestaurantId);
                parameters.Add("@VendorId", callerVendorId); // Logged-in Vendor Id
                parameters.Add("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(model.Password));
                parameters.Add("@ModifiedBy", callerVendorId);
                parameters.Add("@Action", "SET_PASSWORD");

                await connection.ExecuteAsync(
                    "sp_RestaurantCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Password updated successfully.";

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = ex.Message;
                return StatusCode(500, response);
            }
        }
    }
}