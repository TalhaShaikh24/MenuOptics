using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using MenuOptics.Models.DTOs;
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

                var invoices = (await connection.QueryAsync<GetInventoryInvoiceDTO>(
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
                            inv.Items = System.Text.Json.JsonSerializer.Deserialize<List<GetInvoiceItemDTO>>(
                                inv.ItemsJson,
                                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                            ) ?? new List<GetInvoiceItemDTO>();
                        }
                        catch
                        {
                            inv.Items = new List<GetInvoiceItemDTO>();
                        }
                    }
                    else
                    {
                        inv.Items = new List<GetInvoiceItemDTO>();
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
        public async Task<ResponseDto> CreateInvoiceAsync(CreateInvoiceInventoryDTO model, int currentUserId)
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
                    decimal quantity = 0;
                    decimal? unitPrice = null;

                    if (item.Weight != null && item.Weight != 0)
                    {
                        quantity = item.Weight.Value;
                        unitPrice = item.Weight.Value / item.TotalPrice;
                    }
                    else if (item.Quantity != null && item.Quantity != 0)
                    {
                        quantity = item.Quantity.Value;
                        unitPrice = item.Quantity.Value / item.TotalPrice;
                    }
                    else
                    {
                        quantity = 1;
                    }

                    subtotal += item.TotalPrice;

                    table.Rows.Add(
                        (item.IngredientId.HasValue && item.IngredientId.Value > 0) ? (object)item.IngredientId.Value : DBNull.Value,
                        !string.IsNullOrWhiteSpace(item.VendorProductId) ? (object)item.VendorProductId : DBNull.Value,
                        !string.IsNullOrWhiteSpace(item.Name) ? (object)item.Name : DBNull.Value,
                        !string.IsNullOrWhiteSpace(item.Unit) ? (object)item.Unit : DBNull.Value,
                        item.Size.HasValue ? (object)item.Size.Value : DBNull.Value,
                        item.PackSize.HasValue ? (object)item.PackSize.Value : DBNull.Value,
                        quantity,
                        unitPrice,
                        item.TotalPrice
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
                parameters.Add("@UpdatePrices", model.UpdatePrices);
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
        public async Task<ResponseDto> UpdateInvoiceAsync(UpdateInvoiceInventoryDTO model, int currentUserId)
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
                    decimal quantity = 0;
                    decimal unitPrice = 0;

                    if (item.Weight != null && item.Weight != 0)
                    {
                        quantity = item.Weight.Value;
                        unitPrice = item.Weight.Value / item.TotalPrice;
                    }
                    else if (item.Quantity != null && item.Quantity != 0)
                    {
                        quantity = item.Quantity.Value;
                        unitPrice = item.Quantity.Value / item.TotalPrice;
                    }
                    else
                    {
                        quantity = 1;
                    }

                    subtotal += item.TotalPrice;

                    table.Rows.Add(
                        (item.IngredientId.HasValue && item.IngredientId.Value > 0) ? (object)item.IngredientId.Value : DBNull.Value,
                        !string.IsNullOrWhiteSpace(item.VendorProductId) ? (object)item.VendorProductId : DBNull.Value,
                        !string.IsNullOrWhiteSpace(item.Name) ? (object)item.Name : DBNull.Value,
                        !string.IsNullOrWhiteSpace(item.Unit) ? (object)item.Unit : DBNull.Value,
                        item.Size.HasValue ? (object)item.Size.Value : DBNull.Value,
                        item.PackSize.HasValue ? (object)item.PackSize.Value : DBNull.Value,
                        quantity,
                        unitPrice,
                        item.TotalPrice
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
                parameters.Add("@UpdatePrices", model.UpdatePrices);
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
