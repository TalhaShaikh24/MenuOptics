using Dapper;
using MenuOptics.Models;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Tests.IntegrationTests
{
    public class UserSpTests : StoredProcedureTestBase
    {
        [Fact]
        public async Task Sp_UserCRUD_SelectUser_ReturnsCorrectUser()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT");
                selectParams.Add("@UserId", TestUserId);

                var user = await conn.QueryFirstOrDefaultAsync<dynamic>("sp_UserCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(user);
                Assert.Equal((long)TestUserId, (long)user.UserId);
            });
        }

        [Fact]
        public async Task Sp_UserCRUD_DuplicateUsername_ThrowsException()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                // Query existing username
                var existingUsername = await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT TOP 1 Username FROM Users WHERE UserId = @UserId", 
                    new { UserId = TestUserId });

                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@Username", existingUsername); // Duplicate!
                insertParams.Add("@Email", $"test_{Guid.NewGuid():N}@test.com");
                insertParams.Add("@Phone", "1234567890");

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                {
                    await conn.ExecuteScalarAsync<int>("sp_UserCRUD", insertParams, commandType: CommandType.StoredProcedure);
                });

                Assert.Contains("Username already exists", ex.Message);
            });
        }
    }
}
