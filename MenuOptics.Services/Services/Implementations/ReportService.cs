using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;

namespace MenuOptics.Services
{
    public class ReportService : IReportService
    {
        private readonly DapperContext _context;

        public ReportService(DapperContext context)
        {
            _context = context;
        }

        public async Task<ResponseDto> CogsReportAsync(CogsReportRequestDto model, int currentUserId)
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
                parameters.Add("@StartDate", model.StartDate);
                parameters.Add("@EndDate", model.EndDate);

                using var multi = await connection.QueryMultipleAsync("sp_InventorySuiteReport", parameters, commandType: CommandType.StoredProcedure);

                var reportItems = (await multi.ReadAsync<CogsReportItemDto>()).ToList();
                var totalCogsSummary = await multi.ReadFirstOrDefaultAsync<TotalCogsReportItemDto>();

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Inventory report generated successfully.";
                ResponseDto.Data = new
                {
                    Items = reportItems,
                    Summary = totalCogsSummary
                };

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
