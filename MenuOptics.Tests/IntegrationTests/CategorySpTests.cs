using Dapper;
using MenuOptics.Models;
using MenuOptics.Services;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Tests.IntegrationTests
{
    public class CategorySpTests : StoredProcedureTestBase
    {
        [Fact]
        public async Task Sp_CategoryCRUD_InsertAndSelect_ReturnsInsertedCategory()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var uniqueName = $"TestCat_{Guid.NewGuid():N}".Substring(0, 30);

                // 1. Insert via SP
                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@Name", uniqueName);
                insertParams.Add("@RestaurantId", TestRestaurantId);
                insertParams.Add("@CreatedBy", TestUserId);
                insertParams.Add("@IsActive", true);

                var categoryId = await conn.ExecuteScalarAsync<int>("sp_CategoryCRUD", insertParams, commandType: CommandType.StoredProcedure);
                Assert.True(categoryId > 0);

                // 2. Select via SP
                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT");
                selectParams.Add("@CategoryId", categoryId);

                var category = await conn.QueryFirstOrDefaultAsync<Category>("sp_CategoryCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(category);
                Assert.Equal(uniqueName, category.Name);
                Assert.True(category.IsActive);
            });
        }

        [Fact]
        public async Task Sp_CategoryCRUD_DuplicateName_ThrowsException()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var uniqueName = $"TestDup_{Guid.NewGuid():N}".Substring(0, 30);

                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@Name", uniqueName);
                insertParams.Add("@RestaurantId", TestRestaurantId);
                insertParams.Add("@CreatedBy", TestUserId);
                insertParams.Add("@IsActive", true);

                // First insert succeeds
                await conn.ExecuteScalarAsync<int>("sp_CategoryCRUD", insertParams, commandType: CommandType.StoredProcedure);

                // Second insert with same name must fail with RAISERROR
                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                {
                    await conn.ExecuteScalarAsync<int>("sp_CategoryCRUD", insertParams, commandType: CommandType.StoredProcedure);
                });

                Assert.Contains("Category already exists", ex.Message);
            });
        }

        [Fact]
        public async Task Sp_CategoryCRUD_Update_ModifiesCategorySuccessfully()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var initialName = $"TestInit_{Guid.NewGuid():N}".Substring(0, 30);
                var updatedName = $"TestUpd_{Guid.NewGuid():N}".Substring(0, 30);

                // 1. Insert
                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@Name", initialName);
                insertParams.Add("@RestaurantId", TestRestaurantId);
                insertParams.Add("@CreatedBy", TestUserId);
                insertParams.Add("@IsActive", true);

                var categoryId = await conn.ExecuteScalarAsync<int>("sp_CategoryCRUD", insertParams, commandType: CommandType.StoredProcedure);

                // 2. Update
                var updateParams = new DynamicParameters();
                updateParams.Add("@Action", "UPDATE");
                updateParams.Add("@CategoryId", categoryId);
                updateParams.Add("@Name", updatedName);
                updateParams.Add("@ModifiedBy", TestUserId);

                await conn.ExecuteScalarAsync<int>("sp_CategoryCRUD", updateParams, commandType: CommandType.StoredProcedure);

                // 3. Verify
                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT");
                selectParams.Add("@CategoryId", categoryId);

                var category = await conn.QueryFirstOrDefaultAsync<Category>("sp_CategoryCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(category);
                Assert.Equal(updatedName, category.Name);
            });
        }

        [Fact]
        public async Task Sp_CategoryCRUD_ToggleActive_FlipsActiveState()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var name = $"TestToggle_{Guid.NewGuid():N}".Substring(0, 30);

                // 1. Insert Active Category
                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@Name", name);
                insertParams.Add("@RestaurantId", TestRestaurantId);
                insertParams.Add("@CreatedBy", TestUserId);
                insertParams.Add("@IsActive", true);

                var categoryId = await conn.ExecuteScalarAsync<int>("sp_CategoryCRUD", insertParams, commandType: CommandType.StoredProcedure);

                // 2. Toggle Active
                var toggleParams = new DynamicParameters();
                toggleParams.Add("@Action", "TOGGLE_ACTIVE");
                toggleParams.Add("@CategoryId", categoryId);
                toggleParams.Add("@ModifiedBy", TestUserId);

                await conn.ExecuteScalarAsync<int>("sp_CategoryCRUD", toggleParams, commandType: CommandType.StoredProcedure);

                // 3. Check status is now inactive (false)
                var checkParams = new DynamicParameters();
                checkParams.Add("@Action", "SELECT");
                checkParams.Add("@CategoryId", categoryId);

                var category = await conn.QueryFirstOrDefaultAsync<Category>("sp_CategoryCRUD", checkParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(category);
                Assert.False(category.IsActive);
            });
        }

        [Fact]
        public async Task Sp_CategoryCRUD_SelectNonExistent_ThrowsException()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT");
                selectParams.Add("@CategoryId", -99999);

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                {
                    await conn.QueryFirstOrDefaultAsync<Category>("sp_CategoryCRUD", selectParams, commandType: CommandType.StoredProcedure);
                });

                Assert.Contains("Category not found", ex.Message);
            });
        }

        [Fact]
        public async Task CategoryService_EndToEnd_CreateAndFetchWithRollback()
        {
            await RunInRollbackScopeAsync(async () =>
            {
                var service = new CategoryService(DapperContext);
                var uniqueName = $"SvcCat_{Guid.NewGuid():N}".Substring(0, 30);

                var createResult = await service.CreateCategoryAsync(
                    new Category { Name = uniqueName },
                    currentUserId: TestRestaurantId,
                    actualUserId: TestUserId
                );

                Assert.Equal(200, createResult.Status);

                var allResult = await service.GetAllCategoriesAsync(TestRestaurantId);
                Assert.Equal(200, allResult.Status);

                var categories = allResult.Data as IEnumerable<Category>;
                Assert.NotNull(categories);
                Assert.Contains(categories, c => c.Name == uniqueName);
            });
        }
    }
}
