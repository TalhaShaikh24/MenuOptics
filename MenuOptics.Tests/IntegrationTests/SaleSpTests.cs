using Dapper;
using MenuOptics.Models;
using System.Data;

namespace MenuOptics.Tests.IntegrationTests
{
    public class SaleSpTests : StoredProcedureTestBase
    {
        [Fact]
        public async Task Sp_SaleCRUD_InsertAndSelect_CreatesSaleSuccessfully()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@RestaurantId", TestRestaurantId);
                insertParams.Add("@StartDate", DateTime.UtcNow.AddDays(-1));
                insertParams.Add("@EndDate", DateTime.UtcNow);
                insertParams.Add("@Subtotal", 100.00m);
                insertParams.Add("@Tax", 13.00m);
                insertParams.Add("@Total", 113.00m);
                insertParams.Add("@CreatedBy", TestUserId);

                var saleId = await conn.ExecuteScalarAsync<int>("sp_SaleCRUD", insertParams, commandType: CommandType.StoredProcedure);
                Assert.True(saleId > 0);

                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT_ALL");
                selectParams.Add("@RestaurantId", TestRestaurantId);

                var sales = await conn.QueryAsync<dynamic>("sp_SaleCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(sales);
                Assert.Contains(sales, (dynamic s) => (int)s.SaleId == saleId);
            });
        }

        [Fact]
        public async Task Sp_SaleCRUD_SelectAll_ReturnsSalesList()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT_ALL");
                selectParams.Add("@RestaurantId", TestRestaurantId);

                var sales = await conn.QueryAsync<Sale>("sp_SaleCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(sales);
            });
        }
    }
}
