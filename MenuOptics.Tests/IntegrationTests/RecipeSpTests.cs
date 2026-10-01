using Dapper;
using MenuOptics.Models;
using MenuOptics.Services;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Tests.IntegrationTests
{
    public class RecipeSpTests : StoredProcedureTestBase
    {
        [Fact]
        public async Task Sp_RecipeCRUD_InsertAndSelect_ReturnsCorrectData()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var recipeName = $"TestRecipe_{Guid.NewGuid():N}".Substring(0, 30);

                // 1. Insert via SP
                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@RestaurantId", TestRestaurantId);
                insertParams.Add("@Name", recipeName);
                insertParams.Add("@Unit", "Portion");
                insertParams.Add("@Cost", 15.50);
                insertParams.Add("@Size", 1.0);
                insertParams.Add("@Description", "Integration Test Recipe");
                insertParams.Add("@CreatedBy", TestUserId);

                var recipeId = await conn.ExecuteScalarAsync<int>("sp_RecipeCRUD", insertParams, commandType: CommandType.StoredProcedure);
                Assert.True(recipeId > 0);

                // 2. Select via SP
                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT");
                selectParams.Add("@RecipeId", recipeId);

                var recipe = await conn.QueryFirstOrDefaultAsync<Recipe>("sp_RecipeCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(recipe);
                Assert.Equal(recipeName, recipe.Name);
                Assert.Equal("Portion", recipe.Unit);
                Assert.Equal(15.50, recipe.Cost);
            });
        }

        [Fact]
        public async Task Sp_RecipeCRUD_Update_UpdatesFieldsCorrectly()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var initialName = $"RecInit_{Guid.NewGuid():N}".Substring(0, 30);
                var updatedName = $"RecUpd_{Guid.NewGuid():N}".Substring(0, 30);

                // 1. Insert
                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@RestaurantId", TestRestaurantId);
                insertParams.Add("@Name", initialName);
                insertParams.Add("@Unit", "Kg");
                insertParams.Add("@Size", 1.0);
                insertParams.Add("@Cost", 25.00);
                insertParams.Add("@CreatedBy", TestUserId);

                var recipeId = await conn.ExecuteScalarAsync<int>("sp_RecipeCRUD", insertParams, commandType: CommandType.StoredProcedure);

                // 2. Update
                var updateParams = new DynamicParameters();
                updateParams.Add("@Action", "UPDATE");
                updateParams.Add("@RecipeId", recipeId);
                updateParams.Add("@Name", updatedName);
                updateParams.Add("@Cost", 35.00);
                updateParams.Add("@ModifiedBy", TestUserId);

                var rowsAffected = await conn.ExecuteScalarAsync<int>("sp_RecipeCRUD", updateParams, commandType: CommandType.StoredProcedure);
                Assert.True(rowsAffected > 0);

                // 3. Verify
                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT");
                selectParams.Add("@RecipeId", recipeId);

                var recipe = await conn.QueryFirstOrDefaultAsync<Recipe>("sp_RecipeCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(recipe);
                Assert.Equal(updatedName, recipe.Name);
                Assert.Equal(35.00, recipe.Cost);
            });
        }

        [Fact]
        public async Task Sp_RecipeCRUD_SelectNonExistent_ThrowsRecipeNotFound()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT");
                selectParams.Add("@RecipeId", -99999);

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                {
                    await conn.QueryFirstOrDefaultAsync<Recipe>("sp_RecipeCRUD", selectParams, commandType: CommandType.StoredProcedure);
                });

                Assert.Contains("Recipe not found", ex.Message);
            });
        }

        [Fact]
        public async Task Sp_RecipeCRUD_SelectAll_ReturnsRecipesForRestaurant()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var name = $"RecAll_{Guid.NewGuid():N}".Substring(0, 30);

                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@RestaurantId", TestRestaurantId);
                insertParams.Add("@Name", name);
                insertParams.Add("@Unit", "Kg");
                insertParams.Add("@Size", 1.0);
                insertParams.Add("@Cost", 10.0);
                insertParams.Add("@CreatedBy", TestUserId);

                await conn.ExecuteScalarAsync<int>("sp_RecipeCRUD", insertParams, commandType: CommandType.StoredProcedure);

                var selectAllParams = new DynamicParameters();
                selectAllParams.Add("@Action", "SELECT_ALL");
                selectAllParams.Add("@RestaurantId", TestRestaurantId);

                var recipes = await conn.QueryAsync<dynamic>("sp_RecipeCRUD", selectAllParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(recipes);
                Assert.Contains(recipes, (dynamic r) => (string)r.Name == name);
            });
        }

        [Fact]
        public async Task RecipeService_EndToEnd_GetAllRecipes_ReturnsOk()
        {
            await RunInRollbackScopeAsync(async () =>
            {
                var service = new RecipeService(DapperContext);
                var response = await service.GetAllRecipesAsync(TestRestaurantId);

                Assert.Equal(200, response.Status);
                Assert.NotNull(response.Data);
            });
        }
    }
}
