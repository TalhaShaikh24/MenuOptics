using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using Newtonsoft.Json;
using System.Data;

namespace MenuOptics.Services
{
    public class MenuService : IMenuService
    {
        private readonly DapperContext _context;
        private readonly IWebHostEnvironment _environment;

        public MenuService(DapperContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<ResponseDto> CreateMenuItemAsync(Menu model, string ingredients, string recipes, int currentUserId)
        {
            var ResponseDto = new ResponseDto();
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

                string featuredImagePath = null;

                if (model.FeaturedImageFile != null && model.FeaturedImageFile.Length > 0)
                {
                    featuredImagePath = await SaveFeaturedImageAsync(model.FeaturedImageFile);
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
                        ingredientParams.Add("@Uom", ingredient.Uom);
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

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Menu item created successfully.";
                ResponseDto.Data = new { MenuItemId = menuItemId };
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> GetMenuItemAsync(int id)
        {
            var ResponseDto = new ResponseDto();
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
                    ResponseDto.Status = 404;
                    ResponseDto.ResponseMsg = "Menu item not found.";
                    return ResponseDto;
                }

                var ingredients = await connection.QueryAsync<MenuItemIngredient>(
                    "SELECT mi.MenuItemId, mi.IngredientId, i.Name AS IngredientName, mi.Uom, mi.Quantity, i.Unit " +
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

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Menu item retrieved successfully.";
                ResponseDto.Data = menuItem;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> GetAllMenuItemsByAdminAsync()
        {
            var ResponseDto = new ResponseDto();
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

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Menu items retrieved successfully.";
                ResponseDto.Data = menuItems;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> GetAllMenuItemsAsync(int currentUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
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

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Menu items retrieved successfully.";
                ResponseDto.Data = menuItems;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> UpdateMenuItemAsync(Menu model, string ingredients, string recipes, int currentUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (model.MenuItemId <= 0)
                {
                    ResponseDto.Status = 400;
                    ResponseDto.ResponseMsg = "MenuItemId is required for updating a menu item.";
                    return ResponseDto;
                }

                if (!string.IsNullOrWhiteSpace(ingredients))
                {
                    model.Ingredients = JsonConvert.DeserializeObject<List<MenuItemIngredient>>(ingredients);
                }
                if (!string.IsNullOrWhiteSpace(recipes))
                {
                    model.Recipes = JsonConvert.DeserializeObject<List<MenuItemRecipe>>(recipes);
                }

                string featuredImagePath = model.FeaturedImage;

                if (model.FeaturedImageFile != null && model.FeaturedImageFile.Length > 0)
                {
                    featuredImagePath = await SaveFeaturedImageAsync(model.FeaturedImageFile);
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
                    ResponseDto.Status = 404;
                    ResponseDto.ResponseMsg = "Menu item not found or no changes were made.";
                    return ResponseDto;
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
                        ingredientParams.Add("@Uom", ingredient.Uom);
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

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Menu item updated successfully.";
                ResponseDto.Data = new { MenuItemId = model.MenuItemId };
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> ToggleMenuActiveAsync(int id, int currentUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@MenuItemId", id);
                parameters.Add("@Action", "SELECT");

                var menuItem = await connection.QueryFirstOrDefaultAsync<Menu>("sp_MenuCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (menuItem == null)
                {
                    ResponseDto.Status = 404;
                    ResponseDto.ResponseMsg = "Category not found.";
                    return ResponseDto;
                }

                parameters = new DynamicParameters();
                parameters.Add("@MenuItemId", id);
                parameters.Add("@IsActive", !menuItem.IsActive);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "TOGGLE_ACTIVE");

                await connection.ExecuteAsync("sp_MenuCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = $"Menu Item {(menuItem.IsActive ? "deactivated" : "activated")} successfully.";
                ResponseDto.Data = new { MenuItemId = id, IsActive = !menuItem.IsActive };
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        private async Task<string> SaveFeaturedImageAsync(IFormFile featuredImageFile)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "menu");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }
            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(featuredImageFile.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await featuredImageFile.CopyToAsync(fileStream);
            }
            return $"/menu/{uniqueFileName}";
        }
    }
}
