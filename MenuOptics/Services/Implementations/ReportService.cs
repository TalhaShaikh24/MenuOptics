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

        public async Task<Response> CogsReportAsync(CogsReportRequest model, int currentUserId)
        {
            var response = new Response();
            try
            {
                if (currentUserId == 0)
                {
                    response.Status = 401;
                    response.ResponseMsg = "Unauthorized";
                    return response;
                }

                using var connection = _context.CreateConnection();
                connection.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@StartDate", model.StartDate);
                parameters.Add("@EndDate", model.EndDate);

                using var multi = await connection.QueryMultipleAsync("sp_InventorySuiteReport", parameters, commandType: CommandType.StoredProcedure);

                var reportItems = (await multi.ReadAsync<CogsReportItem>()).ToList();
                var totalCogsSummary = await multi.ReadFirstOrDefaultAsync<TotalCogsReportItem>();

                response.Status = 200;
                response.ResponseMsg = "Inventory report generated successfully.";
                response.Data = new
                {
                    Items = reportItems,
                    Summary = totalCogsSummary
                };

                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return response;
            }
        }
    }
}
