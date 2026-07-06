using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using MenuOptics.Data;
using MenuOptics.Models;
using System;
using System.Data;
using System.Security.Claims;

namespace MenuOptics.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecipeApiController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly IConfiguration _configuration;

        public RecipeApiController(DapperContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("CreateRecipe")]
        public async Task<IActionResult> CreateRecipe([FromForm] Recipe model, [FromForm] string ingredients)
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
                        IngredientName = null
                    }).ToList();
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@Unit", model.Unit);
                parameters.Add("@Price", 0);
                parameters.Add("@CreatedBy", currentUserId);
                parameters.Add("@Action", "INSERT");

                int recipeId = await connection.ExecuteScalarAsync<int>("sp_RecipeCRUD", parameters, commandType: CommandType.StoredProcedure);

                double totalPrice = 0;
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

                        double ingPrice = ingDetails.Price;
                        double quantity = (double)ing.Quantity;

                        // No unit conversion — direct multiply
                        totalPrice += ingPrice * quantity;

                        var ingParams = new DynamicParameters();
                        ingParams.Add("@RecipeId", recipeId);
                        ingParams.Add("@IngredientId", ing.IngredientId);
                        ingParams.Add("@Quantity", ing.Quantity);
                        ingParams.Add("@Action", "INSERT");

                        await connection.ExecuteAsync("sp_RecipeIngredientsCRUD", ingParams, commandType: CommandType.StoredProcedure);
                    }
                }

                // Update total price
                var updateParams = new DynamicParameters();
                updateParams.Add("@RecipeId", recipeId);
                updateParams.Add("@UserId", currentUserId);
                updateParams.Add("@Price", totalPrice);
                updateParams.Add("@Action", "UPDATE");
                await connection.ExecuteAsync("sp_RecipeCRUD", updateParams, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Recipe created successfully.";
                response.Data = new { RecipeId = recipeId };
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        [HttpPut("UpdateRecipe")]
        public async Task<IActionResult> UpdateRecipe([FromForm] Recipe model, [FromForm] string ingredients)
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
                    return BadRequest(response);
                }

                if (!string.IsNullOrWhiteSpace(ingredients))
                {
                    var ingredientList = JsonConvert.DeserializeObject<List<RecipeIngredient>>(ingredients);
                    model.Ingredients = ingredientList.Select(i => new RecipeIngredient
                    {
                        IngredientId = i.IngredientId,
                        Quantity = i.Quantity,
                        Unit = i.Unit,
                        IngredientName = null
                    }).ToList();
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var parameters = new DynamicParameters();
                parameters.Add("@RecipeId", model.RecipeId);
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@Unit", model.Unit);
                parameters.Add("@Price", 0);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "UPDATE");

                int affectedRows = await connection.ExecuteAsync("sp_RecipeCRUD", parameters, commandType: CommandType.StoredProcedure);
                if (affectedRows == 0)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Recipe not found.";
                    return NotFound(response);
                }

                // Delete old ingredients
                var deleteParams = new DynamicParameters();
                deleteParams.Add("@RecipeId", model.RecipeId);
                deleteParams.Add("@Action", "DELETE_ALL");
                await connection.ExecuteAsync("sp_RecipeIngredientsCRUD", deleteParams, commandType: CommandType.StoredProcedure);

                double totalPrice = 0;
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

                        double ingPrice = ingDetails.Price;
                        double quantity = (double)ing.Quantity;

                        // Direct cost calculation — no conversion
                        totalPrice += ingPrice * quantity;

                        var ingParams = new DynamicParameters();
                        ingParams.Add("@RecipeId", model.RecipeId);
                        ingParams.Add("@IngredientId", ing.IngredientId);
                        ingParams.Add("@Quantity", ing.Quantity);
                        ingParams.Add("@Unit", ing.Unit);
                        ingParams.Add("@Action", "INSERT");

                        await connection.ExecuteAsync("sp_RecipeIngredientsCRUD", ingParams, commandType: CommandType.StoredProcedure);
                    }
                }

                // Update recipe price
                var updateParams = new DynamicParameters();
                updateParams.Add("@RecipeId", model.RecipeId);
                updateParams.Add("@UserId", currentUserId);
                updateParams.Add("@Price", totalPrice);
                updateParams.Add("@Action", "UPDATE");
                await connection.ExecuteAsync("sp_RecipeCRUD", updateParams, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Recipe updated successfully.";
                response.Data = new { RecipeId = model.RecipeId };
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        // Baaki methods (Get, Delete, Toggle) same rahenge — unme conversion nahi tha
        // Sirf Create aur Update me change kiya gaya hai

        [HttpGet("GetRecipe/{id}")]
        public async Task<IActionResult> GetRecipe(int id)
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
                    return NotFound(response);
                }

                var ingredients = await connection.QueryAsync<RecipeIngredient>(
                    @"SELECT ri.RecipeId, ri.IngredientId, i.Name AS IngredientName, ri.Quantity
                      FROM RecipeIngredients ri
                      INNER JOIN Ingredients i ON i.IngredientId = ri.IngredientId
                      WHERE ri.RecipeId = @RecipeId",
                    new { RecipeId = id });

                recipe.Ingredients = ingredients.ToList();
                response.Status = 200;
                response.ResponseMsg = "Recipe retrieved successfully.";
                response.Data = recipe;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        [HttpGet("GetAllRecipes")]
        public async Task<IActionResult> GetAllRecipes()
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
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
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        [HttpDelete("DeleteRecipe/{id}")]
        public async Task<IActionResult> DeleteRecipe(int id)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var parameters = new DynamicParameters();
                parameters.Add("@RecipeId", id);
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@Action", "DELETE");

                var affectedRows = await connection.ExecuteAsync("sp_RecipeCRUD", parameters, commandType: CommandType.StoredProcedure);
                if (affectedRows == 0)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Recipe not found.";
                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "Recipe deleted successfully.";
                response.Data = new { RecipeId = id };
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        [HttpPut("ToggleRecipeActive/{id}")]
        public async Task<IActionResult> ToggleRecipeActive(int id)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var parameters = new DynamicParameters();
                parameters.Add("@RecipeId", id);
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@Action", "SELECT");

                var recipe = await connection.QueryFirstOrDefaultAsync<Recipe>("sp_RecipeCRUD", parameters, commandType: CommandType.StoredProcedure);
                if (recipe == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Recipe not found.";
                    return NotFound(response);
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
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }
    }
}