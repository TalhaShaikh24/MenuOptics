using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using MenuOptics.Models.Enums;
using MenuOptics.Services.VendorApi;
using MenuOptics.Services.VendorApiServices.Interfaces;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Services.VendorApi
{
    public class RestaurantService : IRestaurantService
    {
        private readonly DapperContext _context;
        private readonly IVendorActivityLogService _vendorActivityLog;
        private static readonly HashSet<string> _allowedSubscriptionType = new(StringComparer.OrdinalIgnoreCase)
        {
            "CORE","PREMIUM"
        };

        public RestaurantService(DapperContext context, IVendorActivityLogService activityLog)
        {
            _context = context;
            _vendorActivityLog = activityLog;
        }

        public async Task<ResponseDto> SignupRestaurantAsync(CreateRestaurantDTO model, int currentUserId)
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(model);
            var ResponseDto = new ResponseDto();

            if (model.SubscriptionType != null && !_allowedSubscriptionType.Contains(model.SubscriptionType.ToUpper()))
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = $"Invalid Subscribtion Type '{model.SubscriptionType}'. Allowed types: {string.Join(", ", _allowedSubscriptionType)}";
                return (ResponseDto);
            }

            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", model.RestaurantId);
                parameters.Add("@VendorId", currentUserId);
                parameters.Add("@FirstName", model.FirstName);
                parameters.Add("@LastName", model.LastName);
                parameters.Add("@Username", model.Username);
                parameters.Add("@Email", model.Email);
                parameters.Add("@Phone", model.Phone);
                parameters.Add("@RestaurantName", model.RestaurantName);
                parameters.Add("@PasswordHash", BCrypt.Net.BCrypt.HashPassword("dbMF0sgx45FD0nVb"));
                parameters.Add("@SubscriptionType", model.SubscriptionType);
                parameters.Add("@SubscriptionStatus", "Active");
                parameters.Add("@PaidBy", PaidBy.VENDOR.ToString());
                parameters.Add("@CreatedBy", currentUserId);
                parameters.Add("@Action", "INSERT");

                int newId = await connection.ExecuteScalarAsync<int>(
                    "sp_RestaurantCRUD", parameters,
                    commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Restaurant signed up successfully.";
                ResponseDto.Data = new { restaurantId = newId };

                await _vendorActivityLog.LogAsync(currentUserId, "POST /api/RestaurantApi/SignupRestaurant",
                   payload, System.Text.Json.JsonSerializer.Serialize(new { ResponseDto.Status, ResponseDto.ResponseMsg, ResponseDto.Data }), "Success");

                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 503;
                ResponseDto.ResponseMsg = $"Exception: {sqlEx.Message}";

                await _vendorActivityLog.LogAsync(currentUserId, "POST /api/RestaurantApi/SignupRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Error");
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";

                await _vendorActivityLog.LogAsync(currentUserId, "POST /api/RestaurantApi/SignupRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Error");
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> UpdateRestaurantAsync(UpdateRestaurantDTO model, int currentUserId)
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(model);
            var ResponseDto = new ResponseDto();

            if (model.SubscriptionType != null && !_allowedSubscriptionType.Contains(model.SubscriptionType.ToUpper()))
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = $"Invalid Subscribtion Type '{model.SubscriptionType}'. Allowed types: {string.Join(", ", _allowedSubscriptionType)}";
                return (ResponseDto);
            }

            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (model.RestaurantId <= 0)
                {
                    ResponseDto.Status = 400;
                    ResponseDto.ResponseMsg = "RestaurantId is required for update.";

                    await _vendorActivityLog.LogAsync(currentUserId, "PUT /api/RestaurantApi/UpdateRestaurant",
                        payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Warning");

                    return ResponseDto;
                }


                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", model.RestaurantId);
                parameters.Add("@VendorId", currentUserId);
                parameters.Add("@FirstName", model.FirstName);
                parameters.Add("@LastName", model.LastName);
                parameters.Add("@Email", model.Email);
                parameters.Add("@Phone", model.Phone);
                parameters.Add("@RestaurantName", model.RestaurantName);
                parameters.Add("@SubscriptionType", model.SubscriptionType);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "UPDATE");

                var updated = await connection.QueryFirstOrDefaultAsync<UpdateRestaurantDTO>(
                    "sp_RestaurantCRUD", parameters,
                    commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Restaurant updated successfully.";
                ResponseDto.Data = updated;

                await _vendorActivityLog.LogAsync(currentUserId, "PUT /api/RestaurantApi/UpdateRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Success");

                return ResponseDto;
            }
            catch (SqlException sqlEx) when (sqlEx.State == 1)//All other SQL Exceptions
            {
                ResponseDto.Status = 503;
                ResponseDto.ResponseMsg = $"Exception: {sqlEx.Message}";

                await _vendorActivityLog.LogAsync(currentUserId, "PUT /api/RestaurantApi/UpdateRestaurant",
                        payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Error");

                return ResponseDto;
            }
            catch (SqlException sqlEx) when (sqlEx.State == 2) //Access denied SQL Exception
            {
                ResponseDto.Status = 403;
                ResponseDto.ResponseMsg = $"{sqlEx.Message}";

                await _vendorActivityLog.LogAsync(currentUserId, "PUT /api/RestaurantApi/UpdateRestaurant",
                        payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Warning");

                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";

                await _vendorActivityLog.LogAsync(currentUserId, "PUT /api/RestaurantApi/UpdateRestaurant",
                    payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Error");
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> ToggleRestaurantActiveAsync(int restaurantId, RestaurantStatusRequestDTO request, int currentUserId)
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(request);
            var ResponseDto = new ResponseDto();

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {

                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", restaurantId);
                parameters.Add("@VendorId", currentUserId);
                parameters.Add("@IsActive", request.IsActive);
                parameters.Add("@ModifiedBy", currentUserId);
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
                    request.IsActive
                };

                await _vendorActivityLog.LogAsync(currentUserId, $"PUT /api/RestaurantApi/ToggleRestaurantActive/{restaurantId}",
                    payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Success");

                return ResponseDto;
            }
            catch (SqlException sqlEx) when (sqlEx.State == 1)//All other SQL Exceptions
            {
                ResponseDto.Status = 503;
                ResponseDto.ResponseMsg = $"Exception: {sqlEx.Message}";

                await _vendorActivityLog.LogAsync(currentUserId, $"PUT /api/RestaurantApi/ToggleRestaurantActive/{restaurantId}",
                        payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Error");

                return ResponseDto;
            }
            catch (SqlException sqlEx) when (sqlEx.State == 2) //Access denied SQL Exception
            {
                ResponseDto.Status = 403;
                ResponseDto.ResponseMsg = $"{sqlEx.Message}";

                await _vendorActivityLog.LogAsync(currentUserId, $"PUT /api/RestaurantApi/ToggleRestaurantActive/{restaurantId}",
                        payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Warning");

                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";

                await _vendorActivityLog.LogAsync(currentUserId, $"PUT /api/RestaurantApi/ToggleRestaurantActive/{restaurantId}",
                        payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Error");

                return ResponseDto;
            }
        }
    }
}