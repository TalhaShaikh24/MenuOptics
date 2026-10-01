using Dapper;
using System.Data;

namespace MenuOptics.Tests.IntegrationTests
{
    public class AnalyticsInsightsSpTests : StoredProcedureTestBase
    {
        [Fact]
        public async Task Sp_GetRestaurantAnalytics_CatchesSchemaColumnMismatch()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", TestUserId);
                parameters.Add("@StartDate", DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd"));
                parameters.Add("@EndDate", DateTime.UtcNow.ToString("yyyy-MM-dd"));

                // NOTE: In DB, sp_GetRestaurantAnalytics queries Sales.UserId which does not exist in Sales table (column is RestaurantId)
                var ex = await Assert.ThrowsAsync<System.Data.SqlClient.SqlException>(async () =>
                {
                    await conn.QueryFirstOrDefaultAsync<dynamic>(
                        "sp_GetRestaurantAnalytics",
                        parameters,
                        commandType: CommandType.StoredProcedure);
                });

                Assert.Contains("UserId", ex.Message);
            });
        }

        [Fact]
        public async Task Sp_GetMenuEngineeringInsights_ExecutesSuccessfully()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", TestRestaurantId);
                parameters.Add("@StartDate", DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd"));
                parameters.Add("@EndDate", DateTime.UtcNow.ToString("yyyy-MM-dd"));

                var result = await conn.QueryAsync<dynamic>(
                    "sp_GetMenuEngineeringInsights",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                Assert.NotNull(result);
            });
        }
    }
}
