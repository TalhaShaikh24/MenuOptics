using Dapper;
using MenuOptics.Models;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Tests.IntegrationTests
{
    public class IngredientSpTests : StoredProcedureTestBase
    {
        [Fact]
        public async Task Sp_IngredientCRUD_InsertAndSelect_ReturnsCorrectIngredient()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var ingredientName = $"Ing_{Guid.NewGuid():N}".Substring(0, 25);

                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@RestaurantId", TestRestaurantId);
                insertParams.Add("@Name", ingredientName);
                insertParams.Add("@Price", 12.50m);
                insertParams.Add("@Unit", "Kg");
                insertParams.Add("@Size", 5.0m);
                insertParams.Add("@PackSize", 1);
                insertParams.Add("@CreatedBy", TestUserId);

                var ingredientId = await conn.ExecuteScalarAsync<int>("sp_IngredientCRUD", insertParams, commandType: CommandType.StoredProcedure);
                Assert.True(ingredientId > 0);

                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT");
                selectParams.Add("@IngredientId", ingredientId);

                var ingredient = await conn.QueryFirstOrDefaultAsync<Ingredient>("sp_IngredientCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(ingredient);
                Assert.Equal(ingredientName, ingredient.Name);
                Assert.Equal(12.50m, ingredient.Price);
                Assert.Equal("Kg", ingredient.Unit);
            });
        }

        [Fact]
        public async Task Sp_IngredientCRUD_MissingName_ThrowsValidationException()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@RestaurantId", TestRestaurantId);
                insertParams.Add("@Name", ""); // Empty name
                insertParams.Add("@Price", 10.0m);
                insertParams.Add("@Size", 1.0m);
                insertParams.Add("@CreatedBy", TestUserId);

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                {
                    await conn.ExecuteScalarAsync<int>("sp_IngredientCRUD", insertParams, commandType: CommandType.StoredProcedure);
                });

                Assert.Contains("Name is required", ex.Message);
            });
        }

        [Fact]
        public async Task Sp_IngredientCRUD_NegativePrice_ThrowsValidationException()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@RestaurantId", TestRestaurantId);
                insertParams.Add("@Name", "Test Ingredient");
                insertParams.Add("@Price", -5.0m); // Negative price
                insertParams.Add("@Size", 1.0m);
                insertParams.Add("@CreatedBy", TestUserId);

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                {
                    await conn.ExecuteScalarAsync<int>("sp_IngredientCRUD", insertParams, commandType: CommandType.StoredProcedure);
                });

                Assert.Contains("Price is required", ex.Message);
            });
        }

        [Fact]
        public async Task Sp_IngredientCRUD_InsertByVendor_NonVendor_ThrowsRestrictedRoleException()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT_BY_VENDOR");
                insertParams.Add("@VendorId", null); // Non vendor
                insertParams.Add("@Name", "Vendor Ingredient");
                insertParams.Add("@Price", 10.0m);
                insertParams.Add("@Size", 1.0m);

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                {
                    await conn.ExecuteScalarAsync<int>("sp_IngredientCRUD", insertParams, commandType: CommandType.StoredProcedure);
                });

                Assert.Contains("restricted to users with the Vendor role", ex.Message);
            });
        }

        [Fact]
        public async Task Sp_IngredientCRUD_SelectNonExistent_ThrowsNotFoundException()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT");
                selectParams.Add("@IngredientId", -88888);

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                {
                    await conn.QueryFirstOrDefaultAsync<Ingredient>("sp_IngredientCRUD", selectParams, commandType: CommandType.StoredProcedure);
                });

                Assert.Contains("Ingredient not found", ex.Message);
            });
        }
    }
}
