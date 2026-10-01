using Dapper;
using MenuOptics.Models;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Tests.IntegrationTests
{
    public class PrivateVendorSpTests : StoredProcedureTestBase
    {
        [Fact]
        public async Task Sp_PrivateVendorCRUD_InsertAndSelect_ReturnsCorrectVendor()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var vendorName = $"Vendor_{Guid.NewGuid():N}".Substring(0, 25);

                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@RestaurantId", TestRestaurantId);
                insertParams.Add("@Name", vendorName);
                insertParams.Add("@CreatedBy", TestUserId);
                insertParams.Add("@IsActive", true);

                var vendorId = await conn.ExecuteScalarAsync<int>("sp_PrivateVendorCRUD", insertParams, commandType: CommandType.StoredProcedure);
                Assert.True(vendorId > 0);

                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT");
                selectParams.Add("@PrivateVendorId", vendorId);

                var vendor = await conn.QueryFirstOrDefaultAsync<PrivateVendor>("sp_PrivateVendorCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(vendor);
                Assert.Equal(vendorName, vendor.Name);
                Assert.True(vendor.IsActive);
            });
        }

        [Fact]
        public async Task Sp_PrivateVendorCRUD_DuplicateName_ThrowsException()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var vendorName = $"DupVen_{Guid.NewGuid():N}".Substring(0, 25);

                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@RestaurantId", TestRestaurantId);
                insertParams.Add("@Name", vendorName);
                insertParams.Add("@CreatedBy", TestUserId);
                insertParams.Add("@IsActive", true);

                await conn.ExecuteScalarAsync<int>("sp_PrivateVendorCRUD", insertParams, commandType: CommandType.StoredProcedure);

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                {
                    await conn.ExecuteScalarAsync<int>("sp_PrivateVendorCRUD", insertParams, commandType: CommandType.StoredProcedure);
                });

                Assert.Contains("Private vendor already exists", ex.Message);
            });
        }

        [Fact]
        public async Task Sp_PrivateVendorCRUD_SelectNonExistent_ThrowsNotFound()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT");
                selectParams.Add("@PrivateVendorId", -77777);

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                {
                    await conn.QueryFirstOrDefaultAsync<PrivateVendor>("sp_PrivateVendorCRUD", selectParams, commandType: CommandType.StoredProcedure);
                });

                Assert.Contains("Private vendor not found", ex.Message);
            });
        }
    }
}
