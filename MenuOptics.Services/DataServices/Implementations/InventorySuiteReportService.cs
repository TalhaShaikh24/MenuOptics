using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using MenuOptics.Models.Entities;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Services
{
    public class InventorySuiteReportService : IInventorySuiteReportService
    {
        private readonly DapperContext _context;

        public InventorySuiteReportService(DapperContext context)
        {
            _context = context;
        }

        public async Task<ResponseDto> InventorySuiteReportAsync(InventorySuiteReportRequestDto model, int currentUserId)
        {
            var ResponseDto = new ResponseDto();

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@InventorySuiteId", model.InventorySuiteId);
                parameters.Add("@StartDate", model.StartDate);
                parameters.Add("@EndDate", model.EndDate);
                parameters.Add("@Action", "SELECT");

                using var multi = await connection.QueryMultipleAsync("sp_InventorySuiteCRUD", parameters, commandType: CommandType.StoredProcedure);

                var reportItems = (await multi.ReadAsync<InventorySuiteReportItemDto>()).ToList();
                var totalCogsSummary = await multi.ReadFirstOrDefaultAsync<TotalInventorySuiteReportItemDto>();

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Inventory report generated successfully.";
                ResponseDto.Data = new
                {
                    Items = reportItems,
                    Summary = totalCogsSummary
                };

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

        public async Task<ResponseDto> GetInventorySuiteReportsAsync(int currentUserId)
        {
            var ResponseDto = new ResponseDto();

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@Action", "SELECT_ALL");

                var inventoryReports = await connection.QueryAsync<InventorySuite>("sp_InventorySuiteCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Inventory Suite reports retrived successfully.";
                ResponseDto.Data = inventoryReports;

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

        public async Task<ResponseDto> UpdateInventorySuiteReportAsync(UpdateInventorySuiteReportRequestDto model, int currentUserId)
        {
            var responseDto = new ResponseDto();

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var table = new DataTable();
                table.Columns.Add("IngredientId", typeof(int));
                table.Columns.Add("ClosingCount", typeof(decimal));
                table.Columns.Add("ClosingValue", typeof(decimal));

                if (model.Items != null)
                {
                    foreach (var item in model.Items)
                    {
                        table.Rows.Add(item.IngredientId, item.ClosingCount, item.ClosingValue);
                    }
                }

                var parameters = new DynamicParameters();
                parameters.Add("@InventorySuiteId", model.InventorySuiteId);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "UPDATE");
                parameters.Add(
                    "@InventorySuiteItems",
                    table.AsTableValuedParameter("dbo.InventorySuiteItemsType")
                );

                var result = await connection.QueryFirstOrDefaultAsync<int>(
                    "sp_InventorySuiteCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Inventory suite report updated successfully.";
                responseDto.Data = new { InventorySuiteId = result };
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
                responseDto.ResponseMsg = $"{ex.Message}";
                return responseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> AddInventorySuiteReportAsync(AddInventorySuiteReportRequestDto model, int currentUserId)
        {
            var responseDto = new ResponseDto();

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var table = new DataTable();
                table.Columns.Add("IngredientId", typeof(int));
                table.Columns.Add("ClosingCount", typeof(decimal));
                table.Columns.Add("ClosingValue", typeof(decimal));

                if (model.Items != null)
                {
                    foreach (var item in model.Items)
                    {
                        table.Rows.Add(item.IngredientId, item.ClosingCount, item.ClosingValue);
                    }
                }

                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@StartDate", model.StartDate);
                parameters.Add("@EndDate", model.EndDate);
                parameters.Add("@CreatedBy", currentUserId);
                parameters.Add("@Action", "INSERT");
                parameters.Add(
                    "@InventorySuiteItems",
                    table.AsTableValuedParameter("dbo.InventorySuiteItemsType")
                );

                var result = await connection.QueryFirstOrDefaultAsync<int>(
                    "sp_InventorySuiteCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Inventory suite report created successfully.";
                responseDto.Data = new { InventorySuiteId = result };
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
                responseDto.ResponseMsg = $"{ex.Message}";
                return responseDto;
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
