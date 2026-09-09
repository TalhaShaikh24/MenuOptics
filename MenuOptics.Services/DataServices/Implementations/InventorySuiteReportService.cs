using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
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
                parameters.Add("@StartDate", model.StartDate);
                parameters.Add("@EndDate", model.EndDate);

                using var multi = await connection.QueryMultipleAsync("sp_InventorySuiteReport", parameters, commandType: CommandType.StoredProcedure);

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
    }
}
