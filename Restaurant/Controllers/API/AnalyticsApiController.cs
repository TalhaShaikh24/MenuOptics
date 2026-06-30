using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;
using System.Threading.Tasks;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Restaurant")]

    public class AnalyticsApiController : ControllerBase
    {
        private readonly DapperContext _context;

        public AnalyticsApiController(DapperContext context)
        {
            _context = context;
        }

        [HttpGet("GetAnalyticsDashboard")]
        public async Task<IActionResult> GetAnalyticsDashboard(DateTime? startDate, DateTime? endDate)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var parameters = new DynamicParameters();

                parameters.Add("@UserId", currentUserId);
                parameters.Add("@StartDate", startDate);
                parameters.Add("@EndDate", endDate);
                

                using var multi = await connection.QueryMultipleAsync(
                    "usp_GetRestaurantAnalytics",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var analytics = new AnalyticsDashboard();

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
                analytics.RevenueTrends = multi.Read<RevenueTrend>().ToList();

                // 6. Pie Chart (multiple rows)
                analytics.RevenueDistribution = multi.Read<PieChartData>().ToList();

                // 7. Menu Performance (multiple rows)
                analytics.MenuPerformances = multi.Read<MenuPerformance>().ToList();

                if (analytics.CurrentRevenue == null && analytics.CurrentSalesCount == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "No analytics data found for the specified user.";
                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "Analytics dashboard data retrieved successfully.";
                response.Data = analytics;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

    }


}
