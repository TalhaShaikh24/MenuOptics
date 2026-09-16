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
    }
}
