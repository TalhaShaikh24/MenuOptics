using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuApiController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public MenuApiController(DapperContext context, IWebHostEnvironment environment, IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _configuration = configuration;
        }

        [HttpPost("CreateMenuItem")]
        public async Task<IActionResult> CreateMenuItem([FromForm] Menu model, [FromForm] string ingredients, [FromForm] string recipes)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (!string.IsNullOrWhiteSpace(ingredients))
                {
                    model.Ingredients = JsonConvert.DeserializeObject<List<MenuItemIngredient>>(ingredients);
                }
                if (!string.IsNullOrWhiteSpace(recipes))
                {
                    model.Recipes = JsonConvert.DeserializeObject<List<MenuItemRecipe>>(recipes);
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                string featuredImagePath = null;

                if (model.FeaturedImageFile != null && model.FeaturedImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "menu");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.FeaturedImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.FeaturedImageFile.CopyToAsync(fileStream);
                    }
                    featuredImagePath = $"/menu/{uniqueFileName}";
                }

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@Description", model.Description);
                parameters.Add("@CategoryId", model.CategoryId);
                parameters.Add("@Cost", model.Cost);
                parameters.Add("@SellingPrice", model.SellingPrice);
                parameters.Add("@FeaturedImage", featuredImagePath);
                parameters.Add("@IsAvailable", model.IsAvailable);
                parameters.Add("@CreatedBy", currentUserId);
                parameters.Add("@Action", "INSERT");

                int menuItemId = await connection.ExecuteScalarAsync<int>("sp_MenuCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (model.Ingredients != null && model.Ingredients.Any())
                {
                    foreach (var ingredient in model.Ingredients)
                    {
                        var ingredientParams = new DynamicParameters();
                        ingredientParams.Add("@MenuItemId", menuItemId);
                        ingredientParams.Add("@IngredientId", ingredient.IngredientId);
                        ingredientParams.Add("@Quantity", ingredient.Quantity);
                        ingredientParams.Add("@Action", "INSERT");
                        await connection.ExecuteAsync("sp_MenuItemIngredientsCRUD", ingredientParams, commandType: CommandType.StoredProcedure);
                    }
                }

                if (model.Recipes != null && model.Recipes.Any())
                {
                    foreach (var recipe in model.Recipes)
                    {
                        var recipeParams = new DynamicParameters();
                        recipeParams.Add("@MenuItemId", menuItemId);
                        recipeParams.Add("@RecipeId", recipe.RecipeId);
                        recipeParams.Add("@Quantity", recipe.Quantity);
                        recipeParams.Add("@Action", "INSERT");
                        await connection.ExecuteAsync("sp_MenuItemRecipesCRUD", recipeParams, commandType: CommandType.StoredProcedure);
                    }
                }

                response.Status = 200;
                response.ResponseMsg = "Menu item created successfully.";
                response.Data = new { MenuItemId = menuItemId };
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

        [HttpGet("GetMenuItem/{id}")]
        public async Task<IActionResult> GetMenuItem(int id)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@MenuItemId", id);
                parameters.Add("@Action", "SELECT");

                var menuItem = await connection.QueryFirstOrDefaultAsync<Menu>(
                    "sp_MenuCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                if (menuItem == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Menu item not found.";
                    return NotFound(response);
                }

                var ingredients = await connection.QueryAsync<MenuItemIngredient>(
                    "SELECT mi.MenuItemId, mi.IngredientId, i.Name AS IngredientName, mi.Quantity, i.Unit " +
                    "FROM MenuItemIngredients mi INNER JOIN Ingredients i ON i.IngredientId = mi.IngredientId " +
                    "WHERE mi.MenuItemId = @MenuItemId",
                    new { MenuItemId = id });

                var recipesQuery = await connection.QueryAsync<MenuItemRecipe>(
                    "SELECT mr.MenuItemId, mr.RecipeId, r.Name AS RecipeName, mr.Quantity, r.Unit " +
                    "FROM MenuItemRecipes mr INNER JOIN Recipes r ON r.RecipeId = mr.RecipeId " +
                    "WHERE mr.MenuItemId = @MenuItemId",
                    new { MenuItemId = id });

                menuItem.Ingredients = ingredients.ToList();
                menuItem.Recipes = recipesQuery.ToList();

                response.Status = 200;
                response.ResponseMsg = "Menu item retrieved successfully.";
                response.Data = menuItem;
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

        [HttpGet("GetAllMenuItemsByAdmin")]
        public async Task<IActionResult> GetAllMenuItemsByAdmin()
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL_ADMIN");

                var menuItems = await connection.QueryAsync<Menu, string, Menu>(
                    "sp_MenuCRUD",
                    (menu, ingredientsJson) =>
                    {
                        menu.Ingredients = !string.IsNullOrEmpty(ingredientsJson)
                            ? System.Text.Json.JsonSerializer.Deserialize<List<MenuItemIngredient>>(ingredientsJson)
                            : new List<MenuItemIngredient>();
                        return menu;
                    },
                    parameters,
                    splitOn: "Ingredients",
                    commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Menu items retrieved successfully.";
                response.Data = menuItems;
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

        [HttpGet("GetAllMenuItems")]
        public async Task<IActionResult> GetAllMenuItems()
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

                var menuItems = await connection.QueryAsync<Menu, string, string, Menu>(
                    "sp_MenuCRUD",
                    (menu, ingredientsJson, recipesJson) =>
                    {
                        menu.Ingredients = !string.IsNullOrEmpty(ingredientsJson)
                            ? System.Text.Json.JsonSerializer.Deserialize<List<MenuItemIngredient>>(ingredientsJson)
                            : new List<MenuItemIngredient>();
                        menu.Recipes = !string.IsNullOrEmpty(recipesJson)
                            ? System.Text.Json.JsonSerializer.Deserialize<List<MenuItemRecipe>>(recipesJson)
                            : new List<MenuItemRecipe>();
                        return menu;
                    },
                    parameters,
                    splitOn: "Ingredients,Recipes",
                    commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Menu items retrieved successfully.";
                response.Data = menuItems;
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

        [HttpPut("UpdateMenuItem")]
        public async Task<IActionResult> UpdateMenuItem([FromForm] Menu model, [FromForm] string ingredients, [FromForm] string recipes)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (model.MenuItemId <= 0)
                {
                    response.Status = 400;
                    response.ResponseMsg = "MenuItemId is required for updating a menu item.";
                    return BadRequest(response);
                }

                if (!string.IsNullOrWhiteSpace(ingredients))
                {
                    model.Ingredients = JsonConvert.DeserializeObject<List<MenuItemIngredient>>(ingredients);
                }
                if (!string.IsNullOrWhiteSpace(recipes))
                {
                    model.Recipes = JsonConvert.DeserializeObject<List<MenuItemRecipe>>(recipes);
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                string featuredImagePath = model.FeaturedImage;

                if (model.FeaturedImageFile != null && model.FeaturedImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "menu");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.FeaturedImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.FeaturedImageFile.CopyToAsync(fileStream);
                    }
                    featuredImagePath = $"/menu/{uniqueFileName}";
                }

                var parameters = new DynamicParameters();
                parameters.Add("@MenuItemId", model.MenuItemId);
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@Description", model.Description);
                parameters.Add("@CategoryId", model.CategoryId);
                parameters.Add("@Cost", model.Cost);
                parameters.Add("@SellingPrice", model.SellingPrice);
                parameters.Add("@FeaturedImage", featuredImagePath);
                parameters.Add("@IsAvailable", model.IsAvailable);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@IsActive", true);
                parameters.Add("@IngredientsJson", ingredients);
                parameters.Add("@RecipesJson", recipes);
                parameters.Add("@Action", "UPDATE");

                int affectedRows = await connection.ExecuteAsync("sp_MenuCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (affectedRows == 0)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Menu item not found or no changes were made.";
                    return NotFound(response);
                }

                var deleteParams = new DynamicParameters();
                deleteParams.Add("@MenuItemId", model.MenuItemId);
                deleteParams.Add("@Action", "DELETE_ALL");
                await connection.ExecuteAsync("sp_MenuItemIngredientsCRUD", deleteParams, commandType: CommandType.StoredProcedure);

                var deleteRecipeParams = new DynamicParameters();
                deleteRecipeParams.Add("@MenuItemId", model.MenuItemId);
                deleteRecipeParams.Add("@Action", "DELETE_ALL");
                await connection.ExecuteAsync("sp_MenuItemRecipesCRUD", deleteRecipeParams, commandType: CommandType.StoredProcedure);

                if (model.Ingredients != null && model.Ingredients.Any())
                {
                    foreach (var ingredient in model.Ingredients)
                    {
                        var ingredientParams = new DynamicParameters();
                        ingredientParams.Add("@MenuItemId", model.MenuItemId);
                        ingredientParams.Add("@IngredientId", ingredient.IngredientId);
                        ingredientParams.Add("@Quantity", ingredient.Quantity);
                        ingredientParams.Add("@Action", "INSERT");
                        await connection.ExecuteAsync("sp_MenuItemIngredientsCRUD", ingredientParams, commandType: CommandType.StoredProcedure);
                    }
                }

                if (model.Recipes != null && model.Recipes.Any())
                {
                    foreach (var recipe in model.Recipes)
                    {
                        var recipeParams = new DynamicParameters();
                        recipeParams.Add("@MenuItemId", model.MenuItemId);
                        recipeParams.Add("@RecipeId", recipe.RecipeId);
                        recipeParams.Add("@Quantity", recipe.Quantity);
                        recipeParams.Add("@Action", "INSERT");
                        await connection.ExecuteAsync("sp_MenuItemRecipesCRUD", recipeParams, commandType: CommandType.StoredProcedure);
                    }
                }

                response.Status = 200;
                response.ResponseMsg = "Menu item updated successfully.";
                response.Data = new { MenuItemId = model.MenuItemId };
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

        [HttpDelete("DeleteMenuItem/{id}")]
        public async Task<IActionResult> DeleteMenuItem(int id)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@MenuItemId", id);
                parameters.Add("@Action", "DELETE");

                var affectedRows = await connection.ExecuteAsync("sp_MenuCRUD", parameters, commandType: CommandType.StoredProcedure);
                if (affectedRows == 0)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Menu item not found.";
                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "Menu item deleted successfully.";
                response.Data = new { MenuItemId = id };
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


        [HttpPut("ToggleMenuActive/{id}")]
        public async Task<IActionResult> ToggleMenuActive(int id)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var parameters = new DynamicParameters();
                parameters.Add("@MenuItemId", id);
                parameters.Add("@Action", "SELECT");

                var menuItem = await connection.QueryFirstOrDefaultAsync<Menu>("sp_MenuCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (menuItem == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Category not found.";
                    return NotFound(response);
                }

                parameters = new DynamicParameters();
                parameters.Add("@MenuItemId", id);
                parameters.Add("@IsActive", !menuItem.IsActive);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "TOGGLE_ACTIVE");

                await connection.ExecuteAsync("sp_MenuCRUD", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = $"Menu Item {(menuItem.IsActive ? "deactivated" : "activated")} successfully.";
                response.Data = new { MenuItemId = id, IsActive = !menuItem.IsActive };
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