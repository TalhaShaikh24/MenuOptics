using Dapper;
using MenuOptics.Models;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Tests.IntegrationTests
{
    public class MenuSpTests : StoredProcedureTestBase
    {
        [Fact]
        public async Task Sp_MenuCRUD_SelectAll_ReturnsMenuItems()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT_ALL");
                selectParams.Add("@RestaurantId", TestRestaurantId);

                var items = await conn.QueryAsync<dynamic>("sp_MenuCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(items);
            });
        }

        [Fact]
        public async Task Sp_MenuCRUD_SelectNonExistent_ThrowsNotFoundException()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT");
                selectParams.Add("@MenuItemId", -99999);
                selectParams.Add("@RestaurantId", TestRestaurantId);

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                {
                    await conn.QueryFirstOrDefaultAsync<dynamic>("sp_MenuCRUD", selectParams, commandType: CommandType.StoredProcedure);
                });

                Assert.Contains("Menu item not found", ex.Message, StringComparison.OrdinalIgnoreCase);
            });
        }
    }
}
