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
        private readonly IIngredientService _ingredientService;

        public InventoryService(DapperContext context,IIngredientService ingredientService , IActivityLogService activityLog)
        {
            _context = context;
            _ingredientService = ingredientService;
            _activityLog = activityLog;
        }

        public async Task<(int StatusCode, object Body)> AddIngredientStockAsync(Inventory model, int currentUserId, int actualUserId)
        {
            var ResponseDto = new ResponseDto();

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {

                Ingredient? ingredient = (Ingredient?)((ResponseDto)(await _ingredientService.GetIngredientAsync(model.IngredientId)).Body).Data;

                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@IngredientId", model.IngredientId);
                parameters.Add("@Stock", model.Stock);
                parameters.Add("@Price", (model.Stock * ingredient?.Price));
                parameters.Add("@CreatedBy", actualUserId);
                parameters.Add("@Action", "INSERT");

                int inventoryId = await connection.ExecuteScalarAsync<int>(
                    "sp_InventoryCRUD", parameters, commandType: CommandType.StoredProcedure
                );

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Stock added successfully.";
                ResponseDto.Data = new { IngredientId = model.IngredientId, InventoryId = inventoryId };

                return (200, ResponseDto);
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return (400, ResponseDto);
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return (500, ResponseDto);
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> UpdateClosingCountAsync(ClosingCountUpdateDto model, int currentUserId)
        {
            var ResponseDto = new ResponseDto();

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
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

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Closing count updated.";
                ResponseDto.Data = new { RowsAffected = rows };
                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
