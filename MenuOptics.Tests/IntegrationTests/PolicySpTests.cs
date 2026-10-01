using Dapper;
using MenuOptics.Models;
using System.Data;

namespace MenuOptics.Tests.IntegrationTests
{
    public class PolicySpTests : StoredProcedureTestBase
    {
        [Fact]
        public async Task Sp_PolicyCRUD_UpsertAndSelect_InsertsAndFetchesPolicy()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var policyType = $"TestType_{Guid.NewGuid():N}".Substring(0, 20);
                var content = "This is test policy content for testing Stored Procedure.";

                // 1. UPSERT (Insert branch)
                var upsertParams = new DynamicParameters();
                upsertParams.Add("@Action", "UPSERT");
                upsertParams.Add("@PolicyType", policyType);
                upsertParams.Add("@Content", content);
                upsertParams.Add("@ModifiedBy", TestUserId);

                await conn.ExecuteAsync("sp_PolicyCRUD", upsertParams, commandType: CommandType.StoredProcedure);

                // 2. SELECT_BY_TYPE
                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT_BY_TYPE");
                selectParams.Add("@PolicyType", policyType);

                var policy = await conn.QueryFirstOrDefaultAsync<PolicyDto>("sp_PolicyCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(policy);
                Assert.Equal(policyType, policy.PolicyType);
                Assert.Equal(content, policy.Content);
            });
        }

        [Fact]
        public async Task Sp_PolicyCRUD_Upsert_UpdatesExistingPolicy()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var policyType = $"UpdType_{Guid.NewGuid():N}".Substring(0, 20);

                // 1. First UPSERT (Insert)
                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "UPSERT");
                insertParams.Add("@PolicyType", policyType);
                insertParams.Add("@Content", "Original content");
                insertParams.Add("@ModifiedBy", TestUserId);

                await conn.ExecuteAsync("sp_PolicyCRUD", insertParams, commandType: CommandType.StoredProcedure);

                // 2. Second UPSERT (Update)
                var updateParams = new DynamicParameters();
                updateParams.Add("@Action", "UPSERT");
                updateParams.Add("@PolicyType", policyType);
                updateParams.Add("@Content", "Updated content");
                updateParams.Add("@ModifiedBy", TestUserId);

                await conn.ExecuteAsync("sp_PolicyCRUD", updateParams, commandType: CommandType.StoredProcedure);

                // 3. SELECT_BY_TYPE
                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT_BY_TYPE");
                selectParams.Add("@PolicyType", policyType);

                var policy = await conn.QueryFirstOrDefaultAsync<PolicyDto>("sp_PolicyCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(policy);
                Assert.Equal("Updated content", policy.Content);
            });
        }

        [Fact]
        public async Task Sp_PolicyCRUD_SelectAll_ReturnsPoliciesList()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT_ALL");

                var policies = await conn.QueryAsync<PolicyDto>("sp_PolicyCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(policies);
            });
        }
    }
}
