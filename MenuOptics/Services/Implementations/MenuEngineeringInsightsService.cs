using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;

namespace MenuOptics.Services
{
    public class MenuEngineeringInsightsService : IMenuEngineeringInsightsService
    {
        private readonly DapperContext _context;

        public MenuEngineeringInsightsService(DapperContext context)
        {
            _context = context;
        }

        public async Task<Response> GetAllMenuEngineeringInsightsByUserIdAsync(int currentUserId, DateTime? startDate, DateTime? endDate)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@StartDate", startDate);
                parameters.Add("@EndDate", endDate);
                parameters.Add("@UserId", currentUserId);

                var insights = await connection.QueryAsync<MenuEngineeringInsights>("sp_GetMenuEngineeringInsightsByUserId", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Menu engineering insights retrieved successfully.";
                response.Data = insights;
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
