using Dapper;
using MenuOptics.Models;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Tests.IntegrationTests
{
    public class ActivityLogSpTests : StoredProcedureTestBase
    {
        [Fact]
        public async Task Sp_ActivityLogCRUD_InsertAndSelect_CreatesLogSuccessfully()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@UserId", TestUserId);
                insertParams.Add("@RequestId", Guid.NewGuid().ToString());
                insertParams.Add("@RequestType", "POST /api/test");
                insertParams.Add("@Payload", "{\"test\": 123}");
                insertParams.Add("@Response", "{\"status\": 200}");
                insertParams.Add("@LogLevel", "Information");
                insertParams.Add("@IpAddress", "127.0.0.1");

                var logId = await conn.ExecuteScalarAsync<int>("sp_ActivityLogCRUD", insertParams, commandType: CommandType.StoredProcedure);
                Assert.True(logId > 0);

                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT");
                selectParams.Add("@LogId", logId);

                var log = await conn.QueryFirstOrDefaultAsync<dynamic>("sp_ActivityLogCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(log);
                Assert.Equal("Information", (string)log.LogLevel);
            });
        }

        [Fact]
        public async Task Sp_ActivityLogCRUD_InvalidUserId_ThrowsException()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@UserId", -99999); // Invalid
                insertParams.Add("@RequestId", Guid.NewGuid().ToString());

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                {
                    await conn.ExecuteScalarAsync<int>("sp_ActivityLogCRUD", insertParams, commandType: CommandType.StoredProcedure);
                });

                Assert.Contains("Invalid VendorId", ex.Message);
            });
        }
    }
}
