using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly DapperContext _context;

        public AnalyticsService(DapperContext context)
        {
            _context = context;
        }

        public async Task<ResponseDto> GetAnalyticsDashboardAsync(int currentUserId, DateTime? startDate, DateTime? endDate)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@StartDate", startDate);
                parameters.Add("@EndDate", endDate);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_GetRestaurantAnalytics",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var analytics = new AnalyticsDashboardDto();

                // 1. Monthly Revenue (3 columns)
                var revenueData = multi.ReadFirstOrDefault<(decimal? Current, decimal? Previous, decimal? Percentage)>();
                analytics.CurrentRevenue = revenueData.Current;
                analytics.PreviousRevenue = revenueData.Previous;
                analytics.PercentageChangeRevenue = revenueData.Percentage;

                // 2. Monthly Sales Count (3 columns)
                var salesData = multi.ReadFirstOrDefault<(int? Current, int? Previous, decimal? Percentage)>();
                analytics.CurrentSalesCount = salesData.Current;
                analytics.PreviousSalesCount = salesData.Previous;
                analytics.PercentageChangeSales = salesData.Percentage;

                // 3a. Average Order Value (3 columns)
                var avgOrderData = multi.ReadFirstOrDefault<(decimal? Current, decimal? Previous, decimal? Percentage)>();
                analytics.CurrentAvgOrderValue = avgOrderData.Current;
                analytics.PreviousAvgOrderValue = avgOrderData.Previous;
                analytics.PercentageChangeAvgOrder = avgOrderData.Percentage;

                // 3b. Total Items Sold (3 columns)
                var itemsSoldData = multi.ReadFirstOrDefault<(int? Current, int? Previous, decimal? Percentage)>();
                analytics.CurrentItemsSold = itemsSoldData.Current;
                analytics.PreviousItemsSold = itemsSoldData.Previous;
                analytics.PercentageChangeItems = itemsSoldData.Percentage;

                // 5. Revenue Trend (multiple rows)
                analytics.RevenueTrends = multi.Read<RevenueTrendDto>().ToList();

                // 6. Pie Chart (multiple rows)
                analytics.RevenueDistribution = multi.Read<PieChartDataDto>().ToList();

                // 7. Menu Performance (multiple rows)
                analytics.MenuPerformances = multi.Read<MenuPerformanceDto>().ToList();

                if (analytics.CurrentRevenue == null && analytics.CurrentSalesCount == null)
                {
                    ResponseDto.Status = 404;
                    ResponseDto.ResponseMsg = "No analytics data found for the specified user.";
                    return ResponseDto;
                }

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Analytics dashboard data retrieved successfully.";
                ResponseDto.Data = analytics;
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
