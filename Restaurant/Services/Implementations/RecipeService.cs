using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using Newtonsoft.Json;
using System.Data;

namespace MenuOptics.Services
{
    public class RecipeService : IRecipeService
    {
        private readonly DapperContext _context;

        public RecipeService(DapperContext context)
        {
            _context = context;
        }

        public async Task<Response> CreateRecipeAsync(Recipe model, string ingredients, int currentUserId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (!string.IsNullOrWhiteSpace(ingredients))
                {
                    var ingredientList = JsonConvert.DeserializeObject<List<RecipeIngredient>>(ingredients);
                    model.Ingredients = ingredientList.Select(i => new RecipeIngredient
                    {
                        IngredientId = i.IngredientId,
                        Quantity = i.Quantity,
                        Unit = i.Unit,
                        Uom = i.Uom,
                        IngredientName = null
                    }).ToList();
                }

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@Unit", model.Unit);
                parameters.Add("@Price", 0);
                parameters.Add("@Description", model.Description);
                parameters.Add("@CreatedBy", currentUserId);
                parameters.Add("@Action", "INSERT");

                int recipeId = await connection.ExecuteScalarAsync<int>("sp_RecipeCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (model.Ingredients != null && model.Ingredients.Any())
                {
                    foreach (var ing in model.Ingredients)
                    {
                        var priceParams = new DynamicParameters();
                        priceParams.Add("@IngredientId", ing.IngredientId);
                        priceParams.Add("@UserId", currentUserId);

                        var ingDetails = await connection.QueryFirstOrDefaultAsync<dynamic>(
                            @"SELECT COALESCE(vip.Price, i.Price) AS Price
                              FROM Ingredients i
                              LEFT JOIN VendorIngredientRestaurantPrices vip
                                ON i.IngredientId = vip.VendorIngredientId AND vip.RestaurantId = @UserId
                              WHERE i.IngredientId = @IngredientId",
                            priceParams);

                        if (ingDetails == null)
                        {
                            throw new Exception($"Ingredient {ing.IngredientId} not found.");
                        }

                       

                        var ingParams = new DynamicParameters();
                        ingParams.Add("@RecipeId", recipeId);
                        ingParams.Add("@IngredientId", ing.IngredientId);
                        ingParams.Add("@Uom", ing.Uom);
                        ingParams.Add("@Quantity", ing.Quantity);
                        ingParams.Add("@Action", "INSERT");

                        await connection.ExecuteAsync("sp_RecipeIngredientsCRUD", ingParams, commandType: CommandType.StoredProcedure);
                    }
                }

                // Update total price
                var updateParams = new DynamicParameters();
                updateParams.Add("@RecipeId", recipeId);
                updateParams.Add("@UserId", currentUserId);
                updateParams.Add("@Price", model.Price);
                updateParams.Add("@Action", "UPDATE");
                await connection.ExecuteAsync("sp_RecipeCRUD", updateParams, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Recipe created successfully.";
                response.Data = new { RecipeId = recipeId };
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> UpdateRecipeAsync(Recipe model, string ingredients, int currentUserId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (model.RecipeId <= 0)
                {
                    response.Status = 400;
                    response.ResponseMsg = "RecipeId is required.";
                    return response;
                }

                if (!string.IsNullOrWhiteSpace(ingredients))
                {
                    var ingredientList = JsonConvert.DeserializeObject<List<RecipeIngredient>>(ingredients);
                    model.Ingredients = ingredientList.Select(i => new RecipeIngredient
                    {
                        IngredientId = i.IngredientId,
                        Quantity = i.Quantity,
                        Uom = i.Uom,
                        Unit = i.Unit,
                        IngredientName = null
                    }).ToList();
                }

                var parameters = new DynamicParameters();
                parameters.Add("@RecipeId", model.RecipeId);
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@Unit", model.Unit);
                parameters.Add("@Price", model.Price);
                parameters.Add("@Description", model.Description);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "UPDATE");

                int affectedRows = await connection.ExecuteAsync("sp_RecipeCRUD", parameters, commandType: CommandType.StoredProcedure);
                if (affectedRows == 0)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Recipe not found.";
                    return response;
                }

                // Delete old ingredients
                var deleteParams = new DynamicParameters();
                deleteParams.Add("@RecipeId", model.RecipeId);
                deleteParams.Add("@Action", "DELETE_ALL");
                await connection.ExecuteAsync("sp_RecipeIngredientsCRUD", deleteParams, commandType: CommandType.StoredProcedure);

                if (model.Ingredients != null && model.Ingredients.Any())
                {
                    foreach (var ing in model.Ingredients)
                    {
                        var priceParams = new DynamicParameters();
                        priceParams.Add("@IngredientId", ing.IngredientId);
                        priceParams.Add("@UserId", currentUserId);

                        var ingDetails = await connection.QueryFirstOrDefaultAsync<dynamic>(
                            @"SELECT COALESCE(vip.Price, i.Price) AS Price
                              FROM Ingredients i
                              LEFT JOIN VendorIngredientRestaurantPrices vip
                                ON i.IngredientId = vip.VendorIngredientId AND vip.RestaurantId = @UserId
                              WHERE i.IngredientId = @IngredientId",
                            priceParams);

                        if (ingDetails == null)
                        {
                            throw new Exception($"Ingredient {ing.IngredientId} not found.");
                        }


                        var ingParams = new DynamicParameters();
                        ingParams.Add("@RecipeId", model.RecipeId);
                        ingParams.Add("@IngredientId", ing.IngredientId);
                        ingParams.Add("@Uom", ing.Uom);
                        ingParams.Add("@Quantity", ing.Quantity);
                        ingParams.Add("@Action", "INSERT");

                        await connection.ExecuteAsync("sp_RecipeIngredientsCRUD", ingParams, commandType: CommandType.StoredProcedure);
                    }
                }

                // Update recipe price
                var updateParams = new DynamicParameters();
                updateParams.Add("@RecipeId", model.RecipeId);
                updateParams.Add("@UserId", currentUserId);
                updateParams.Add("@Price", model.Price);
                parameters.Add("@Description", model.Description);
                updateParams.Add("@Action", "UPDATE");
                await connection.ExecuteAsync("sp_RecipeCRUD", updateParams, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Recipe updated successfully.";
                response.Data = new { RecipeId = model.RecipeId };
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> GetRecipeAsync(int id)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@RecipeId", id);
                parameters.Add("@Action", "SELECT");
                var recipe = await connection.QueryFirstOrDefaultAsync<Recipe>(
                    "sp_RecipeCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (recipe == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Recipe not found.";
                    return response;
                }

                var ingredients = await connection.QueryAsync<RecipeIngredient>(
                    @"SELECT ri.RecipeId, ri.IngredientId, i.Name AS IngredientName, ri.Uom, ri.Quantity
                      FROM RecipeIngredients ri
                      INNER JOIN Ingredients i ON i.IngredientId = ri.IngredientId
                      WHERE ri.RecipeId = @RecipeId",
                    new { RecipeId = id });

                recipe.Ingredients = ingredients.ToList();
                response.Status = 200;
                response.ResponseMsg = "Recipe retrieved successfully.";
                response.Data = recipe;
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> GetAllRecipesAsync(int currentUserId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@Action", "SELECT_ALL");

                var recipes = await connection.QueryAsync<Recipe, string, Recipe>(
                    "sp_RecipeCRUD",
                    (recipe, ingredientsJson) =>
                    {
                        recipe.Ingredients = !string.IsNullOrEmpty(ingredientsJson)
                            ? System.Text.Json.JsonSerializer.Deserialize<List<RecipeIngredient>>(ingredientsJson)
                            : new List<RecipeIngredient>();
                        return recipe;
                    },
                    parameters,
                    splitOn: "Ingredients",
                    commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Recipes retrieved successfully.";
                response.Data = recipes;
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> DeleteRecipeAsync(int id, int currentUserId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@RecipeId", id);
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@Action", "DELETE");

                var affectedRows = await connection.ExecuteAsync("sp_RecipeCRUD", parameters, commandType: CommandType.StoredProcedure);
                if (affectedRows == 0)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Recipe not found.";
                    return response;
                }

                response.Status = 200;
                response.ResponseMsg = "Recipe deleted successfully.";
                response.Data = new { RecipeId = id };
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> ToggleRecipeActiveAsync(int id, int currentUserId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@RecipeId", id);
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@Action", "SELECT");

                var recipe = await connection.QueryFirstOrDefaultAsync<Recipe>("sp_RecipeCRUD", parameters, commandType: CommandType.StoredProcedure);
                if (recipe == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Recipe not found.";
                    return response;
                }

                parameters = new DynamicParameters();
                parameters.Add("@RecipeId", id);
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "TOGGLE_ACTIVE");

                await connection.ExecuteAsync("sp_RecipeCRUD", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = $"Recipe {(recipe.IsActive ? "deactivated" : "activated")} successfully.";
                response.Data = new { RecipeId = id, IsActive = !recipe.IsActive };
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
