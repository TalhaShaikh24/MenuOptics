using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Services
{
    public class RecipeService : IRecipeService
    {
        private readonly DapperContext _context;
        public RecipeService(DapperContext context)
        {
            _context = context;
        }

        public async Task<ResponseDto> CreateRecipeAsync(Recipe model, string ingredients, int currentUserId, int actualUserId)
        {
            var ResponseDto = new ResponseDto();
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
                        UOM = i.UOM,
                        IngredientName = null
                    }).ToList();
                }

                var ingredientTable = new DataTable();
                ingredientTable.Columns.Add("IngredientId", typeof(int));
                ingredientTable.Columns.Add("UOM", typeof(string));
                ingredientTable.Columns.Add("Quantity", typeof(decimal));

                if (model.Ingredients != null && model.Ingredients.Any())
                {
                    foreach (var ingredient in model.Ingredients)
                    {
                        ingredientTable.Rows.Add(ingredient.IngredientId, ingredient.UOM.ToUpper(), ingredient.Quantity);
                    }
                }

                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@Unit", model.Unit.ToUpper());
                parameters.Add("@Cost", model.Cost);
                parameters.Add("@Size", model.Size);
                parameters.Add("@Description", model.Description);
                parameters.Add("@CreatedBy", actualUserId);
                parameters.Add("@RecipeIngredients", ingredientTable.AsTableValuedParameter("dbo.RecipeIngredientsType"));
                parameters.Add("@Action", "INSERT");

                int recipeId = await connection.ExecuteScalarAsync<int>("sp_RecipeCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Recipe created successfully.";
                ResponseDto.Data = new { RecipeId = recipeId };
                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> UpdateRecipeAsync(Recipe model, string ingredients, int currentUserId, int actualUserId)
        {
            var ResponseDto = new ResponseDto();
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
                        UOM = i.UOM,
                        Unit = i.Unit,
                        IngredientName = null
                    }).ToList();
                }

                var ingredientTable = new DataTable();
                ingredientTable.Columns.Add("IngredientId", typeof(int));
                ingredientTable.Columns.Add("UOM", typeof(string));
                ingredientTable.Columns.Add("Quantity", typeof(decimal));

                if (model.Ingredients != null && model.Ingredients.Any())
                {
                    foreach (var ingredient in model.Ingredients)
                    {
                        ingredientTable.Rows.Add(ingredient.IngredientId, ingredient.UOM.ToUpper(), ingredient.Quantity);
                    }
                }

                var parameters = new DynamicParameters();
                parameters.Add("@RecipeId", model.RecipeId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@Unit", model.Unit.ToUpper());
                parameters.Add("@Cost", model.Cost);
                parameters.Add("@Size", model.Size);
                parameters.Add("@Description", model.Description);
                parameters.Add("@ModifiedBy", actualUserId);
                parameters.Add("@RecipeIngredients", ingredientTable.AsTableValuedParameter("dbo.RecipeIngredientsType"));
                parameters.Add("@Action", "UPDATE");

                int updatedRecipeId = await connection.ExecuteAsync("sp_RecipeCRUD", parameters, commandType: CommandType.StoredProcedure);
                
                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Recipe updated successfully.";
                ResponseDto.Data = new { RecipeId = updatedRecipeId };
                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> GetRecipeAsync(int id)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@RecipeId", id);
                parameters.Add("@Action", "SELECT");

                var multi = await connection.QueryMultipleAsync(
                    "sp_RecipeCRUD", parameters, commandType: CommandType.StoredProcedure);

                var recipe = await multi.ReadFirstAsync<Recipe>();
                recipe.Ingredients = (await multi.ReadAsync<RecipeIngredient>()).ToList();

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Recipe retrieved successfully.";
                ResponseDto.Data = recipe;
                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> GetAllRecipesAsync(int currentUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
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

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Recipes retrieved successfully.";
                ResponseDto.Data = recipes;
                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> ToggleRecipeActiveAsync(int id, int currentUserId, int actualUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();

                parameters = new DynamicParameters();
                parameters.Add("@RecipeId", id);
                parameters.Add("@ModifiedBy", actualUserId);
                parameters.Add("@Action", "TOGGLE_ACTIVE");

                var result = await connection.QueryFirstAsync("sp_RecipeCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = $"Recipe {(result.IsActive ? "activated" : "deactivated")} successfully.";
                ResponseDto.Data = new 
                { 
                    RecipeId = id, 
                    IsActive = result.IsActive 
                };
                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
