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

        public async Task<Response> GetMyRestaurantsAsync(string? nameIdentifierClaim, string requestId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var callerVendorId = GetCallerVendorId(nameIdentifierClaim);
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

                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";

                await _activityLog.LogAsync(null, requestId, "GET /api/RestaurantApi/GetMyRestaurants",
                    null, System.Text.Json.JsonSerializer.Serialize(response), "Error");

                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> GetMyRestaurantAsync(int id, string? nameIdentifierClaim, string requestId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var callerVendorId = GetCallerVendorId(nameIdentifierClaim);
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

                    return response;
                }

                response.Status = 200;
                response.ResponseMsg = "Restaurant retrieved successfully.";
                response.Data = restaurant;

                await _activityLog.LogAsync(callerVendorId, requestId, "GET /api/RestaurantApi/GetMyRestaurant",
                    id.ToString(), System.Text.Json.JsonSerializer.Serialize(response), "Success");

                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";

                await _activityLog.LogAsync(null, requestId, "GET /api/RestaurantApi/GetMyRestaurant",
                    id.ToString(), System.Text.Json.JsonSerializer.Serialize(response), "Error");

                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> SignupRestaurantAsync(CreateRestaurant model, string? nameIdentifierClaim, string requestId)
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(model);
            var response = new Response();

            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var callerVendorId = GetCallerVendorId(nameIdentifierClaim);

                if (callerVendorId == 0)
                {
                    response.Status = 401;
                    response.ResponseMsg = "Unauthorized";
                    await _activityLog.LogAsync(null, requestId, "POST /api/RestaurantApi/SignupRestaurant",
                        payload, System.Text.Json.JsonSerializer.Serialize(response), "Warning");
                    return response;
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

                return response;
            }
            catch (SqlException sqlEx)
            {
                response.Status = 503;
                response.ResponseMsg = $"Exception: {sqlEx.Message}";

                await _activityLog.LogAsync(null, requestId, "POST /api/RestaurantApi/SignupRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(response), "Error");
                return response;
            }
            catch (InvalidOperationException invalidOpEx)
            {
                response.Status = 422;
                response.ResponseMsg = $"Invalid operation: {invalidOpEx.Message}";

                await _activityLog.LogAsync(null, requestId, "POST /api/RestaurantApi/SignupRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(response), "Error");
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";

                await _activityLog.LogAsync(null, requestId, "POST /api/RestaurantApi/SignupRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(response), "Error");
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> UpdateRestaurantAsync(UpdateRestaurant model, string? nameIdentifierClaim, string requestId)
        {
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

                    return response;
                }

                var callerVendorId = GetCallerVendorId(nameIdentifierClaim);

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

                    return response;
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

                return response;
            }
            catch (SqlException sqlEx)
            {
                response.Status = 503;
                response.ResponseMsg = $"Exception: {sqlEx.Message}";

                await _activityLog.LogAsync(null, requestId, "PUT /api/RestaurantApi/UpdateRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(response), "Error");
                return response;
            }
            catch (InvalidOperationException invalidOpEx)
            {
                response.Status = 422;
                response.ResponseMsg = $"Invalid operation: {invalidOpEx.Message}";

                await _activityLog.LogAsync(null, requestId, "PUT /api/RestaurantApi/UpdateRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(response), "Error");
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";

                await _activityLog.LogAsync(null, requestId, "PUT /api/RestaurantApi/UpdateRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(response), "Error");
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> ToggleRestaurantActiveAsync(int restaurantId, RestaurantStatusRequest request, string? nameIdentifierClaim, string requestId)
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(request);
            var response = new Response();

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var callerVendorId = GetCallerVendorId(nameIdentifierClaim);

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

                    return response;
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

                return response;
            }
            catch (SqlException sqlEx)
            {
                response.Status = 503;
                response.ResponseMsg = $"Exception: {sqlEx.Message}";
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return response;
            }
        }

        public async Task<Response> SetPasswordAsync(SetPasswordDTO model, string? nameIdentifierClaim)
        {
            var response = new Response();
            // Intentionally resolved outside the try block (matches original behavior).
            var callerVendorId = GetCallerVendorId(nameIdentifierClaim);
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

                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = ex.Message;
                return response;
            }
        }
    }
}
