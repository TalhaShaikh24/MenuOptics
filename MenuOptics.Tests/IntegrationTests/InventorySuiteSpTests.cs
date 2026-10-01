using Dapper;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Tests.IntegrationTests
{
    public class InventorySuiteSpTests : StoredProcedureTestBase
    {
        [Fact]
        public async Task Sp_InventorySuiteCRUD_InvalidRestaurant_ThrowsError()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@RestaurantId", -99999); // Invalid
                insertParams.Add("@StartDate", DateTime.UtcNow.AddDays(-7));
                insertParams.Add("@EndDate", DateTime.UtcNow);
                insertParams.Add("@CreatedBy", TestUserId);

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                {
                    await conn.ExecuteScalarAsync<int>("sp_InventorySuiteCRUD", insertParams, commandType: CommandType.StoredProcedure);
                });

                Assert.Contains("Invalid RestaurantId", ex.Message);
            });
        }

        [Fact]
        public async Task Sp_InventorySuiteCRUD_InsertAndSelect_CreatesSuiteSuccessfully()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@RestaurantId", TestRestaurantId);
                insertParams.Add("@StartDate", DateTime.UtcNow.AddDays(-7));
                insertParams.Add("@EndDate", DateTime.UtcNow);
                insertParams.Add("@CreatedBy", TestUserId);

                var suiteId = await conn.ExecuteScalarAsync<int>("sp_InventorySuiteCRUD", insertParams, commandType: CommandType.StoredProcedure);
                Assert.True(suiteId > 0);

                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT_ALL");
                selectParams.Add("@RestaurantId", TestRestaurantId);

                var suites = await conn.QueryAsync<dynamic>("sp_InventorySuiteCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(suites);
                Assert.Contains(suites, (dynamic s) => (int)s.InventorySuiteId == suiteId);
            });
        }

        [Fact]
        public async Task Sp_InventorySuiteCRUD_SelectAll_ReturnsSuitesList()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT_ALL");
                selectParams.Add("@RestaurantId", TestRestaurantId);

                var suites = await conn.QueryAsync<dynamic>("sp_InventorySuiteCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(suites);
            });
        }
    }
}
