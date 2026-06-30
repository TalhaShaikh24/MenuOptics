using Dapper;
using MenuOptics.Data;
using MenuOptics.Middleware;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryApiController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly IActivityLogService _activityLog;

        public InventoryApiController(DapperContext context, IActivityLogService activityLog)
        {
            _context = context;
            _activityLog = activityLog;
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Authorize(Roles = "Vendor")]
        [HttpPost("InvoiceInventory")]
        public async Task<IActionResult> InvoiceInventory([FromBody] InvoiceInventoryRequest model)
        {
            var response = new Response();
            var requestId = HttpContext.Items[RequestIdMiddleware.RequestIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
            var payload = System.Text.Json.JsonSerializer.Serialize(model);

            if (!ModelState.IsValid)
            {
                response.Status = 400;
                response.ResponseMsg = "Invalid input.";
                response.Data = ModelState;
                await _activityLog.LogAsync(null, requestId, "POST /api/InventoryApi/InvoiceInventory",
                    payload, System.Text.Json.JsonSerializer.Serialize(response), "Warning");
                return BadRequest(response);
            }

            if (model.Items == null || !model.Items.Any())
            {
                response.Status = 400;
                response.ResponseMsg = "At least one item is required.";
                await _activityLog.LogAsync(null, requestId, "POST /api/InventoryApi/InvoiceInventory",
                    payload, System.Text.Json.JsonSerializer.Serialize(response), "Warning");
                return BadRequest(response);
            }

            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (currentUserId == 0)
                {
                    response.Status = 401;
                    response.ResponseMsg = "Unauthorized";
                    await _activityLog.LogAsync(null, requestId, "POST /api/InventoryApi/InvoiceInventory",
                        payload, System.Text.Json.JsonSerializer.Serialize(response), "Warning");
                    return Unauthorized(response);
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

                        decimal price = 0;
                        decimal stock = 0;

                        if (item.Weight != null && item.Weight != 0)
                        {
                            price = item.Price / item.Weight.Value;
                            stock = item.Weight.Value;
                        }
                        else if (item.Quantity != null && item.Quantity != 0
                            && item.PackSize != null && item.PackSize > 0)
                        {
                            price = (item.Price / item.Quantity.Value) / item.PackSize.Value;
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
                        parameters.Add("@Price", price);
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

                response.Status = failedItems.Any()
                    ? (createdItems.Any() ? 207 : 400)
                    : 200;

                response.ResponseMsg = failedItems.Any()
                    ? (createdItems.Any() ? "Inventory invoice recorded successfully." : "Inventory invoice recording failed.")
                    : "Inventory invoice recorded successfully.";

                response.Data = new {Errors = failedItems };

                await _activityLog.LogAsync(currentUserId, requestId, "POST /api/InventoryApi/InvoiceInventory",
                    payload, System.Text.Json.JsonSerializer.Serialize(response), failedItems.Any() ? "Warning" : "Success");

                return response.Status switch
                {
                    400 => BadRequest(response),
                    207 => StatusCode(207, response),
                    _ => Ok(response)
                };
            }
            catch (Exception ex)
            {
                response.Status = 400;
                response.ResponseMsg = $"{ex.Message}";
                await _activityLog.LogAsync(null, requestId, "POST /api/InventoryApi/InvoiceInventory",
                    payload, System.Text.Json.JsonSerializer.Serialize(response), "Error");
                return StatusCode(400, response);
            }
        }

        // ─── unchanged below this line ────────────────────────────────────────────

        [HttpGet("GetInventory/{id}")]
        public async Task<IActionResult> GetInventory(int id)
        {
            var response = new Response();

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
                    response.Status = 404;
                    response.ResponseMsg = "Inventory record not found.";
                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "Inventory record retrieved successfully.";
                response.Data = inventory;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
        }

        [HttpGet("GetAllInventoryByRestaurant/{restaurantId}")]
        public async Task<IActionResult> GetAllInventoryByRestaurant(int restaurantId)
        {
            var response = new Response();

            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (currentUserId == 0)
                {
                    response.Status = 401;
                    response.ResponseMsg = "Unauthorized";
                    return Unauthorized(response);
                }

                using var connection = _context.CreateConnection();
                connection.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", restaurantId);
                parameters.Add("@Action", "SELECT_BY_RESTAURANT");

                var inventory = await connection.QueryAsync<Inventory>(
                    "sp_InventoryCRUD", parameters, commandType: CommandType.StoredProcedure
                );

                response.Status = 200;
                response.ResponseMsg = "Inventory records retrieved successfully.";
                response.Data = inventory;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
        }

        [HttpPost("UpdateClosingCount")]
        public async Task<IActionResult> UpdateClosingCount([FromBody] ClosingCountUpdateDto model)
        {
            var response = new Response();

            if (!ModelState.IsValid)
            {
                response.Status = 400;
                response.ResponseMsg = "Invalid input.";
                return BadRequest(response);
            }

            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (currentUserId == 0)
                {
                    response.Status = 401;
                    response.ResponseMsg = "Unauthorized";
                    return Unauthorized(response);
                }

                using var connection = _context.CreateConnection();
                connection.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@IngredientId", model.IngredientId);
                parameters.Add("@ClosingCount", model.ClosingCount);
                parameters.Add("@ClosingDate", model.ClosingDate);

                int rows = await connection.ExecuteAsync(
                    "sp_InventoryClosingCount_Update",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                if (rows == 0)
                {
                    response.Status = 404;
                    response.ResponseMsg = "No matching inventory record found.";
                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "Closing count updated.";
                response.Data = new { RowsAffected = rows };
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
        }
    }
}