using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using Microsoft.Extensions.Primitives;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly DapperContext _context;
        private readonly IActivityLogService _activityLog;

        public InventoryService(DapperContext context, IActivityLogService activityLog)
        {
            _context = context;
            _activityLog = activityLog;

        }

        public async Task<ResponseDto> GetInventoryAsync(int id)
        {
            var ResponseDto = new ResponseDto();

            try
            {
                using var connection = _context.CreateConnection();
                connection.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@InventoryId", id);
                parameters.Add("@Action", "SELECT");

                var inventory = await connection.QueryFirstOrDefaultAsync<Inventory>(
                    "sp_InventoryCRUD", parameters, commandType: CommandType.StoredProcedure
                );

                if (inventory == null)
                {
                    ResponseDto.Status = 404;
                    ResponseDto.ResponseMsg = "Inventory record not found.";
                    return ResponseDto;
                }

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Inventory record retrieved successfully.";
                ResponseDto.Data = inventory;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";
                return ResponseDto;
            }
        }

        public async Task<ResponseDto> GetAllInventoryByRestaurantAsync(int restaurantId, int currentUserId)
        {
            var ResponseDto = new ResponseDto();

            try
            {
                if (currentUserId == 0)
                {
                    ResponseDto.Status = 401;
                    ResponseDto.ResponseMsg = "Unauthorized";
                    return ResponseDto;
                }

                using var connection = _context.CreateConnection();
                connection.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", restaurantId);
                parameters.Add("@Action", "SELECT_BY_RESTAURANT");

                var inventory = await connection.QueryAsync<Inventory>(
                    "sp_InventoryCRUD", parameters, commandType: CommandType.StoredProcedure
                );

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Inventory records retrieved successfully.";
                ResponseDto.Data = inventory;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";
                return ResponseDto;
            }
        }

        public async Task<ResponseDto> UpdateClosingCountAsync(ClosingCountUpdateDto model, int currentUserId)
        {
            var ResponseDto = new ResponseDto();

            try
            {
                if (currentUserId == 0)
                {
                    ResponseDto.Status = 401;
                    ResponseDto.ResponseMsg = "Unauthorized";
                    return ResponseDto;
                }

                using var connection = _context.CreateConnection();
                connection.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@IngredientId", model.IngredientId);
                parameters.Add("@ClosingCount", model.ClosingCount);
                parameters.Add("@ClosingValue", model.ClosingValue);
                parameters.Add("@ClosingDate", model.ClosingDate);

                int rows = await connection.ExecuteAsync(
                    "sp_InventoryClosingCount_Update",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                if (rows == 0)
                {
                    ResponseDto.Status = 404;
                    ResponseDto.ResponseMsg = "No matching inventory record found.";
                    return ResponseDto;
                }

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Closing count updated.";
                ResponseDto.Data = new { RowsAffected = rows };
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
