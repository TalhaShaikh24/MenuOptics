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

        public async Task<ResponseDto> GetAllMenuEngineeringInsightsByUserIdAsync(int currentUserId, DateTime? startDate, DateTime? endDate)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@StartDate", startDate);
                parameters.Add("@EndDate", endDate);
                parameters.Add("@RestaurantId", currentUserId);

                var insights = await connection.QueryAsync<MenuEngineeringInsightsDto>("sp_GetMenuEngineeringInsights", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Menu engineering insights retrieved successfully.";
                ResponseDto.Data = insights;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
