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
        public InventoryService(DapperContext context)
        {
            _context = context;
        }

        public async Task<ResponseDto> UpdateInventorySuiteReportAsync(InventorySuiteUpdateDto model, int currentUserId)
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

        public async Task<ResponseDto> GetInventoryInvoicesAsync(int currentUserId)
        {
            var responseDto = new ResponseDto();

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@Action", "SELECT_ALL");

                var invoices = (await connection.QueryAsync<InventoryInvoiceDto>(
                    "sp_InventoryInvoiceCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure
                )).ToList();

                foreach (var inv in invoices)
                {
                    if (!string.IsNullOrWhiteSpace(inv.ItemsJson))
                    {
                        try
                        {
                            inv.Items = System.Text.Json.JsonSerializer.Deserialize<List<InventoryInvoiceItemDetailDto>>(
                                inv.ItemsJson,
                                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                            ) ?? new List<InventoryInvoiceItemDetailDto>();
                        }
                        catch
                        {
                            inv.Items = new List<InventoryInvoiceItemDetailDto>();
                        }
                    }
                    else
                    {
                        inv.Items = new List<InventoryInvoiceItemDetailDto>();
                    }
                }

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Invoices retrieved successfully.";
                responseDto.Data = invoices;
                return responseDto;
            }
            catch (SqlException sqlEx)
            {
                responseDto.Status = 400;
                responseDto.ResponseMsg = sqlEx.Message;
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = ex.Message;
                return responseDto;
            }
            finally
            {
                connection.Close();
            }
        }
        public async Task<ResponseDto> CreateInvoiceAsync(CreateInvoiceRequestDto model, int currentUserId)
        {
            var responseDto = new ResponseDto();

            if (model.Items == null || !model.Items.Any())
            {
                responseDto.Status = 400;
                responseDto.ResponseMsg = "At least one item is required.";
                return responseDto;
            }

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                decimal subtotal = 0;
                decimal tax = 0;
                decimal total = 0;

                var table = new DataTable();
                table.Columns.Add("IngredientId", typeof(int));
                table.Columns.Add("Quantity", typeof(decimal));
                table.Columns.Add("PackSize", typeof(int));
                table.Columns.Add("Price", typeof(decimal));

                foreach (var item in model.Items)
                {
                    decimal quantity = 0;

                    if (item.Weight != null && item.Weight != 0)
                    {
                        quantity = item.Weight.Value;
                    }
                    else if (item.Quantity != null && item.Quantity != 0)
                    {
                        quantity = item.Quantity.Value;
                    }
                    else
                    {
                        quantity = 1;
                    }

                    subtotal += quantity * item.Price;

                    table.Rows.Add(
                        item.IngredientId,
                        quantity,
                        item.PackSize.HasValue ? (object)item.PackSize.Value : DBNull.Value,
                        item.Price
                    );
                }

                tax = 0;
                total = subtotal + tax;

                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@PrivateVendorId", model.PrivateVendorId);
                parameters.Add("@InvoiceId", model.InvoiceId);
                parameters.Add("@InvoiceDate", model.InvoiceDate);
                parameters.Add("@CreatedBy", currentUserId);
                parameters.Add("@Subtotal", subtotal);
                parameters.Add("@Tax", tax);
                parameters.Add("@Total", total);
                parameters.Add("@Action", "INSERT");
                parameters.Add(
                    "@InventoryInvoiceItems",
                    table.AsTableValuedParameter("dbo.InventoryInvoiceItemsType")
                );

                var invoiceIds = await connection.QueryAsync<int>(
                    "sp_InventoryInvoiceCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                int newId = invoiceIds.FirstOrDefault();

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Invoice created successfully.";
                responseDto.Data = new { InventoryInvoiceId = newId };
                return responseDto;
            }
            catch (SqlException sqlEx)
            {
                responseDto.Status = 400;
                responseDto.ResponseMsg = sqlEx.Message;
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = ex.Message;
                return responseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> UpdateInvoiceAsync(UpdateInvoiceRequestDto model, int currentUserId)
        {
            var responseDto = new ResponseDto();

            if (model.Items == null || !model.Items.Any())
            {
                responseDto.Status = 400;
                responseDto.ResponseMsg = "At least one item is required.";
                return responseDto;
            }

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                decimal subtotal = 0;
                decimal tax = 0;
                decimal total = 0;

                var table = new DataTable();
                table.Columns.Add("IngredientId", typeof(int));
                table.Columns.Add("Quantity", typeof(decimal));
                table.Columns.Add("PackSize", typeof(int));
                table.Columns.Add("Price", typeof(decimal));

                foreach (var item in model.Items)
                {
                    decimal quantity = 0;

                    if (item.Weight != null && item.Weight != 0)
                    {
                        quantity = item.Weight.Value;
                    }
                    else if (item.Quantity != null && item.Quantity != 0)
                    {
                        quantity = item.Quantity.Value;
                    }
                    else
                    {
                        quantity = 1;
                    }

                    subtotal += quantity * item.Price;

                    table.Rows.Add(
                        item.IngredientId,
                        quantity,
                        item.PackSize.HasValue ? (object)item.PackSize.Value : DBNull.Value,
                        item.Price
                    );
                }

                tax = 0;
                total = subtotal + tax;

                var parameters = new DynamicParameters();
                parameters.Add("@InventoryInvoiceId", model.InventoryInvoiceId);
                parameters.Add("@Subtotal", subtotal);
                parameters.Add("@Tax", tax);
                parameters.Add("@Total", total);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "UPDATE");
                parameters.Add(
                    "@InventoryInvoiceItems",
                    table.AsTableValuedParameter("dbo.InventoryInvoiceItemsType")
                );

                await connection.ExecuteAsync(
                    "sp_InventoryInvoiceCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Invoice updated successfully.";
                responseDto.Data = new { InventoryInvoiceId = model.InventoryInvoiceId };
                return responseDto;
            }
            catch (SqlException sqlEx)
            {
                responseDto.Status = 400;
                responseDto.ResponseMsg = sqlEx.Message;
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = ex.Message;
                return responseDto;
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
