using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using MenuOptics.Services.VendorApiServices.Interfaces;
using Microsoft.Extensions.Primitives;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Services.VendorApi
{
    public class InventoryService : IInventoryService
    {
        private readonly DapperContext _context;
        private readonly IVendorActivityLogService _vendorActivityLog;

        public InventoryService(DapperContext context, IVendorActivityLogService activityLog)
        {
            _context = context;
            _vendorActivityLog = activityLog;

        }

        public async Task<ResponseDto> InvoiceInventoryAsync(CreateVendorInvoiceInventoryDTO model, int currentUserId)
        {
            var ResponseDto = new ResponseDto();
            var payload = System.Text.Json.JsonSerializer.Serialize(model);

            if (model.Items == null || !model.Items.Any())
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = "At least one item is required.";

                await _vendorActivityLog.LogAsync(
                    currentUserId,
                    "POST /api/InventoryApi/InvoiceInventory",
                    payload,
                    System.Text.Json.JsonSerializer.Serialize(ResponseDto),
                    "Warning");

                return ResponseDto;
            }

            try
            {
                using var connection = _context.CreateConnection();
                connection.Open();

                decimal subtotal = 0;
                decimal tax = 0;
                decimal total = 0;

                var createdItems = new List<object>();
                var failedItems = new List<object>();


                var table = new DataTable();

                table.Columns.Add("IngredientId", typeof(int));
                table.Columns.Add("VendorProductId", typeof(string));
                table.Columns.Add("Name", typeof(string));
                table.Columns.Add("Unit", typeof(string));
                table.Columns.Add("Size", typeof(decimal));
                table.Columns.Add("PackSize", typeof(int));
                table.Columns.Add("Quantity", typeof(decimal));
                table.Columns.Add("UnitPrice", typeof(decimal));
                table.Columns.Add("TotalPrice", typeof(decimal));

                foreach (var item in model.Items)
                {
                    try
                    {
                        decimal quantity = 0;

                        if (item.Weight != null && item.Weight != 0)
                        {
                            quantity = item.Weight.Value;
                        }
                        else if (item.Quantity != null && item.Quantity != 0
                            && item.PackSize != null && item.PackSize > 0)
                        {
                            quantity = item.Quantity.Value;
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

                        // Calculate subtotal from quantity × price
                        subtotal += item.Price;

                        table.Rows.Add(
                            item.IngredientId,
                            DBNull.Value,
                            DBNull.Value,
                            DBNull.Value,
                            DBNull.Value,
                            item.PackSize,
                            quantity,
                            DBNull.Value,
                            item.Price
                        );
                    }
                    catch (SqlException sqlEx)
                    {
                        failedItems.Add(new
                        {
                            item.IngredientId,
                            Error = sqlEx.Message
                        });
                    }
                    catch (Exception ex)
                    {
                        failedItems.Add(new
                        {
                            item.IngredientId,
                            Error = ex.Message
                        });
                    }
                }

                if (table.Rows.Count > 0)
                {

                    tax = 0;
                    total = subtotal + tax;

                    var parameters = new DynamicParameters();

                    parameters.Add("@RestaurantId", model.RestaurantId);
                    parameters.Add("@VendorId", currentUserId);
                    parameters.Add("@InvoiceId", model.InvoiceId);
                    parameters.Add("@CreatedBy", currentUserId);

                    parameters.Add("@Subtotal", subtotal);
                    parameters.Add("@Tax", tax);
                    parameters.Add("@Total", total);
                    parameters.Add("@Action", "INSERT");

                    parameters.Add(
                        "@InventoryInvoiceItems",
                        table.AsTableValuedParameter(
                            "dbo.InventoryInvoiceItemsType"
                        )
                    );

                    var inventoryInvoiceId = await connection.QueryAsync<int>(
                        "sp_InventoryInvoiceCRUD",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    foreach (var id in inventoryInvoiceId)
                    {
                        createdItems.Add(new
                        {
                            InventoryInvoiceId = id
                        });
                    }

                }

                ResponseDto.Status = failedItems.Any()
                    ? (createdItems.Any() ? 207 : 400)
                    : 200;

                ResponseDto.ResponseMsg = failedItems.Any()
                    ? (createdItems.Any()
                        ? "Inventory invoice recorded successfully."
                        : "Inventory invoice recording failed.")
                    : "Inventory invoice recorded successfully.";

                ResponseDto.Data = new
                {
                    CreatedItems = createdItems,
                    Errors = failedItems
                };

                await _vendorActivityLog.LogAsync(
                    currentUserId,
                    "POST /api/InventoryApi/InvoiceInventory",
                    payload,
                    System.Text.Json.JsonSerializer.Serialize(ResponseDto),
                    failedItems.Any() ? "Warning" : "Success");

                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = $"{ex.Message}";

                await _vendorActivityLog.LogAsync(
                    currentUserId,
                    "POST /api/InventoryApi/InvoiceInventory",
                    payload,
                    System.Text.Json.JsonSerializer.Serialize(ResponseDto),
                    "Error");

                return ResponseDto;
            }
        }
    }
}
