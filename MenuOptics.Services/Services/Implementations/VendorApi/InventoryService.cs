using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using Microsoft.Extensions.Primitives;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Services.VendorApi
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

        public async Task<ResponseDto> InvoiceInventoryAsync(InvoiceInventoryRequestDto model, int currentUserId, string requestId)
        {
            var ResponseDto = new ResponseDto();
            var payload = System.Text.Json.JsonSerializer.Serialize(model);

            if (model.Items == null || !model.Items.Any())
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = "At least one item is required.";
                await _activityLog.LogAsync(null, requestId, "POST /api/InventoryApi/InvoiceInventory",
                    payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Warning");
                return ResponseDto;
            }

            try
            {
                if (currentUserId == 0)
                {
                    ResponseDto.Status = 401;
                    ResponseDto.ResponseMsg = "Unauthorized";
                    await _activityLog.LogAsync(null, requestId, "POST /api/InventoryApi/InvoiceInventory",
                        payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Warning");
                    return ResponseDto;
                }

                using var connection = _context.CreateConnection();
                connection.Open();

                var createdItems = new List<object>();
                var failedItems = new List<object>();

                foreach (var item in model.Items)
                {
                    try
                    {
                        var parameters = new DynamicParameters();

                        decimal stock = 0;

                        if (item.Weight != null && item.Weight != 0)
                        {
                            stock = item.Weight.Value;
                        }
                        else if (item.Quantity != null && item.Quantity != 0
                            && item.PackSize != null && item.PackSize > 0)
                        {
                            stock = item.Quantity.Value * item.PackSize.Value;
                        }
                        else
                        {
                            failedItems.Add(new
                            {
                                item.IngredientId,
                                Error = "Invalid weight or quantity/pack size."
                            });
                            continue;
                        }

                        parameters.Add("@RestaurantId", model.RestaurantId);
                        parameters.Add("@VendorId", currentUserId);
                        parameters.Add("@IngredientId", item.IngredientId);
                        parameters.Add("@Stock", stock);
                        parameters.Add("@Price", item.Price);
                        parameters.Add("@InvoiceId", model.InvoiceId);
                        parameters.Add("@CreatedBy", currentUserId);
                        parameters.Add("@Action", "INSERT");

                        int inventoryId = await connection.ExecuteScalarAsync<int>(
                            "sp_InventoryCRUD", parameters, commandType: CommandType.StoredProcedure
                        );

                        createdItems.Add(new { item.IngredientId, InventoryId = inventoryId });
                    }
                    catch (SqlException sqlEx)
                    {
                        failedItems.Add(new { item.IngredientId, Error = sqlEx.Message });
                    }
                    catch (Exception ex)
                    {
                        failedItems.Add(new { item.IngredientId, Error = ex.Message });
                    }
                }

                ResponseDto.Status = failedItems.Any()
                    ? (createdItems.Any() ? 207 : 400)
                    : 200;

                ResponseDto.ResponseMsg = failedItems.Any()
                    ? (createdItems.Any() ? "Inventory invoice recorded successfully." : "Inventory invoice recording failed.")
                    : "Inventory invoice recorded successfully.";

                ResponseDto.Data = new { Errors = failedItems };

                await _activityLog.LogAsync(currentUserId, requestId, "POST /api/InventoryApi/InvoiceInventory",
                    payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), failedItems.Any() ? "Warning" : "Success");

                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                await _activityLog.LogAsync(null, requestId, "POST /api/InventoryApi/InvoiceInventory",
                    payload, System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Error");
                return ResponseDto;
            }
        }
    }
}
