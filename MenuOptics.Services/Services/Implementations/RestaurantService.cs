using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Services
{
    public class RestaurantService : IRestaurantService
    {
        private readonly DapperContext _context;
        private readonly IActivityLogService _activityLog;

        public RestaurantService(DapperContext context, IActivityLogService activityLog)
        {
            _context = context;
            _activityLog = activityLog;
        }

        // Resolves the caller's VendorId from the JWT NameIdentifier claim value.
        private static int GetCallerVendorId(string? nameIdentifierClaim)
        {
            if (int.TryParse(nameIdentifierClaim, out int id))
                return id;

            throw new InvalidOperationException("Vendor identity could not be determined from token.");
        }

        public async Task<ResponseDto> GetMyRestaurantsAsync(string? nameIdentifierClaim, string requestId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var callerVendorId = GetCallerVendorId(nameIdentifierClaim);
                var parameters = new DynamicParameters();
                parameters.Add("@VendorId", callerVendorId);
                parameters.Add("@Action", "SELECT_ALL");

                var restaurants = await connection.QueryAsync<UserDto>(
                    "sp_RestaurantCRUD", parameters,
                    commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Restaurants retrieved successfully.";
                ResponseDto.Data = restaurants;

                await _activityLog.LogAsync(callerVendorId, requestId, "GET /api/RestaurantApi/GetMyRestaurants",
                    null, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Success");

                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";

                await _activityLog.LogAsync(null, requestId, "GET /api/RestaurantApi/GetMyRestaurants",
                    null, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Error");

                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> GetMyRestaurantAsync(int id, string? nameIdentifierClaim, string requestId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var callerVendorId = GetCallerVendorId(nameIdentifierClaim);
                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", id);
                parameters.Add("@VendorId", callerVendorId);
                parameters.Add("@Action", "SELECT");

                var restaurant = await connection.QueryFirstOrDefaultAsync<UserDto>(
                    "sp_RestaurantCRUD", parameters,
                    commandType: CommandType.StoredProcedure);

                if (restaurant == null)
                {
                    ResponseDto.Status = 404;
                    ResponseDto.ResponseMsg = "Restaurant not found or you do not have access to it.";

                    await _activityLog.LogAsync(callerVendorId, requestId, "GET /api/RestaurantApi/GetMyRestaurant",
                        id.ToString(), System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Warning");

                    return ResponseDto;
                }

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Restaurant retrieved successfully.";
                ResponseDto.Data = restaurant;

                await _activityLog.LogAsync(callerVendorId, requestId, "GET /api/RestaurantApi/GetMyRestaurant",
                    id.ToString(), System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Success");

                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";

                await _activityLog.LogAsync(null, requestId, "GET /api/RestaurantApi/GetMyRestaurant",
                    id.ToString(), System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Error");

                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> SignupRestaurantAsync(CreateRestaurantDto model, string? nameIdentifierClaim, string requestId)
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(model);
            var ResponseDto = new ResponseDto();

            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var callerVendorId = GetCallerVendorId(nameIdentifierClaim);

                if (callerVendorId == 0)
                {
                    ResponseDto.Status = 401;
                    ResponseDto.ResponseMsg = "Unauthorized";
                    await _activityLog.LogAsync(null, requestId, "POST /api/RestaurantApi/SignupRestaurant",
                        payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Warning");
                    return ResponseDto;
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
                parameters.Add("@SubscriptionStatus", "Active");
                parameters.Add("@CreatedBy", callerVendorId);
                parameters.Add("@Action", "INSERT");

                int newId = await connection.ExecuteScalarAsync<int>(
                    "sp_RestaurantCRUD", parameters,
                    commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Restaurant signed up successfully.";
                ResponseDto.Data = new { restaurantId = newId };

                await _activityLog.LogAsync(callerVendorId, requestId, "POST /api/RestaurantApi/SignupRestaurant",
                   payload, System.Text.Json.JsonSerializer.Serialize(new { ResponseDto.Status, ResponseDto.ResponseMsg, ResponseDto.Data }), "Success");

                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 503;
                ResponseDto.ResponseMsg = $"Exception: {sqlEx.Message}";

                await _activityLog.LogAsync(null, requestId, "POST /api/RestaurantApi/SignupRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Error");
                return ResponseDto;
            }
            catch (InvalidOperationException invalidOpEx)
            {
                ResponseDto.Status = 422;
                ResponseDto.ResponseMsg = $"Invalid operation: {invalidOpEx.Message}";

                await _activityLog.LogAsync(null, requestId, "POST /api/RestaurantApi/SignupRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Error");
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";

                await _activityLog.LogAsync(null, requestId, "POST /api/RestaurantApi/SignupRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Error");
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> UpdateRestaurantAsync(UpdateRestaurantDto model, string? nameIdentifierClaim, string requestId)
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(model);
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (model.RestaurantId <= 0)
                {
                    ResponseDto.Status = 400;
                    ResponseDto.ResponseMsg = "RestaurantId is required for update.";

                    await _activityLog.LogAsync(null, requestId, "PUT /api/RestaurantApi/UpdateRestaurantDto",
                        payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Warning");

                    return ResponseDto;
                }

                var callerVendorId = GetCallerVendorId(nameIdentifierClaim);

                var checkParams = new DynamicParameters();
                checkParams.Add("@RestaurantId", model.RestaurantId);
                checkParams.Add("@VendorId", callerVendorId);
                checkParams.Add("@Action", "SELECT");

                var existing = await connection.QueryFirstOrDefaultAsync<UserDto>(
                    "sp_RestaurantCRUD", checkParams,
                    commandType: CommandType.StoredProcedure);

                if (existing == null)
                {
                    ResponseDto.Status = 403;
                    ResponseDto.ResponseMsg = "Access denied: You can only update your own restaurants.";

                    await _activityLog.LogAsync(callerVendorId, requestId, "PUT /api/RestaurantApi/UpdateRestaurantDto",
                        payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Warning");

                    return ResponseDto;
                }

                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", model.RestaurantId);
                parameters.Add("@VendorId", callerVendorId);
                parameters.Add("@FirstName", model.FirstName);
                parameters.Add("@LastName", model.LastName);
                parameters.Add("@Email", model.Email);
                parameters.Add("@Phone", model.Phone);
                parameters.Add("@RestaurantName", model.RestaurantName);
                parameters.Add("@ModifiedBy", callerVendorId);
                parameters.Add("@Action", "UPDATE");

                var updated = await connection.QueryFirstOrDefaultAsync<UpdateRestaurantDto>(
                    "sp_RestaurantCRUD", parameters,
                    commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Restaurant updated successfully.";
                ResponseDto.Data = updated;

                await _activityLog.LogAsync(callerVendorId, requestId, "PUT /api/RestaurantApi/UpdateRestaurantDto",
                    payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Success");

                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 503;
                ResponseDto.ResponseMsg = $"Exception: {sqlEx.Message}";

                await _activityLog.LogAsync(null, requestId, "PUT /api/RestaurantApi/UpdateRestaurantDto",
                    payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Error");
                return ResponseDto;
            }
            catch (InvalidOperationException invalidOpEx)
            {
                ResponseDto.Status = 422;
                ResponseDto.ResponseMsg = $"Invalid operation: {invalidOpEx.Message}";

                await _activityLog.LogAsync(null, requestId, "PUT /api/RestaurantApi/UpdateRestaurantDto",
                    payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Error");
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";

                await _activityLog.LogAsync(null, requestId, "PUT /api/RestaurantApi/UpdateRestaurantDto",
                    payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Error");
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> ToggleRestaurantActiveAsync(int restaurantId, RestaurantStatusRequestDto request, string? nameIdentifierClaim, string requestId)
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(request);
            var ResponseDto = new ResponseDto();

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var callerVendorId = GetCallerVendorId(nameIdentifierClaim);

                var checkParams = new DynamicParameters();
                checkParams.Add("@RestaurantId", restaurantId);
                checkParams.Add("@VendorId", callerVendorId);
                checkParams.Add("@Action", "SELECT");

                var existing = await connection.QueryFirstOrDefaultAsync<UserDto>(
                    "sp_RestaurantCRUD", checkParams,
                    commandType: CommandType.StoredProcedure);

                if (existing == null)
                {
                    ResponseDto.Status = 403;
                    ResponseDto.ResponseMsg = "Access denied: You can only update your own restaurants.";

                    await _activityLog.LogAsync(callerVendorId, requestId, $"PUT /api/RestaurantApi/ToggleRestaurantActive/{restaurantId}",
                        payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Warning");

                    return ResponseDto;
                }

                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", restaurantId);
                parameters.Add("@VendorId", callerVendorId);
                parameters.Add("@IsActive", request.IsActive);
                parameters.Add("@ModifiedBy", callerVendorId);
                parameters.Add("@Action", "TOGGLE_ACTIVE");

                await connection.ExecuteAsync(
                    "sp_RestaurantCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = $"Restaurant {(request.IsActive ? "activated" : "deactivated")} successfully.";
                ResponseDto.Data = new
                {
                    RestaurantId = restaurantId,
                    IsActive = request.IsActive
                };

                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 503;
                ResponseDto.ResponseMsg = $"Exception: {sqlEx.Message}";
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";
                return ResponseDto;
            }
        }
    }
}