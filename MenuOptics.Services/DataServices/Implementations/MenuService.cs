using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using MenuOptics.Models.DTOs.MenuItem;
using Newtonsoft.Json;
using Stripe;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Services
{
    public class MenuService : IMenuService
    {
        private readonly DapperContext _context;
        private readonly IFileUploadService _fileService;
        private readonly IWebHostEnvironment _environment;

        public MenuService(DapperContext context,  IWebHostEnvironment environment, IFileUploadService fileService)
        {
            _context = context;
            _environment = environment;
            _fileService = fileService;
        }

        public async Task<ResponseDto> CreateMenuItemAsync(CreateMenuItemDTO model, string ingredients, string recipes, int currentUserId, int actualUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (!string.IsNullOrWhiteSpace(ingredients))
                {
                    model.Ingredients = JsonConvert.DeserializeObject<List<SaveMenuItemIngredientDTO>>(ingredients);
                }
                if (!string.IsNullOrWhiteSpace(recipes))
                {
                    model.Recipes = JsonConvert.DeserializeObject<List<SaveMenuItemRecipeDTO>>(recipes);
                }

                string? featuredImagePath = null;

                if (model.FeaturedImageFile != null && model.FeaturedImageFile.Length > 0)
                {
                    featuredImagePath = await _fileService.UploadFileAsync(model.FeaturedImageFile, "menu");
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

                var recipeTable = new DataTable();
                recipeTable.Columns.Add("RecipeId", typeof(int));
                recipeTable.Columns.Add("Quantity", typeof(decimal));

                if (model.Recipes != null && model.Recipes.Any())
                {
                    foreach (var recipe in model.Recipes)
                    {
                        recipeTable.Rows.Add(recipe.RecipeId, recipe.Quantity);
                    }
                }

                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@Description", model.Description);
                parameters.Add("@CategoryId", model.CategoryId);
                parameters.Add("@Cost", model.Cost);
                parameters.Add("@SellingPrice", model.SellingPrice);
                parameters.Add("@FeaturedImage", featuredImagePath);
                parameters.Add("@CreatedBy", actualUserId);
                parameters.Add("@MenuItemIngredients", ingredientTable.AsTableValuedParameter("dbo.MenuItemIngredientsType"));
                parameters.Add("@MenuItemRecipes", recipeTable.AsTableValuedParameter("dbo.MenuItemRecipesType"));
                parameters.Add("@Action", "INSERT");

                int menuItemId = await connection.ExecuteScalarAsync<int>("sp_MenuCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Menu item created successfully.";
                ResponseDto.Data = new { MenuItemId = menuItemId };
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

        public async Task<ResponseDto> UpdateMenuItemAsync(UpdateMenuItemDTO model, string ingredients, string recipes, int currentUserId, int actualUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
               
                if (!string.IsNullOrWhiteSpace(ingredients))
                {
                    model.Ingredients = JsonConvert.DeserializeObject<List<SaveMenuItemIngredientDTO>>(ingredients);
                }

                if (!string.IsNullOrWhiteSpace(recipes))
                {
                    model.Recipes = JsonConvert.DeserializeObject<List<SaveMenuItemRecipeDTO>>(recipes);
                }

                string? featuredImagePath = null;

                if (model.FeaturedImageFile != null && model.FeaturedImageFile.Length > 0)
                {
                    featuredImagePath = await _fileService.UploadFileAsync(model.FeaturedImageFile, "menu");
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

                var recipeTable = new DataTable();
                recipeTable.Columns.Add("RecipeId", typeof(int));
                recipeTable.Columns.Add("Quantity", typeof(decimal));

                if (model.Recipes != null && model.Recipes.Any())
                {
                    foreach (var recipe in model.Recipes)
                    {
                        recipeTable.Rows.Add(recipe.RecipeId, recipe.Quantity);
                    }
                }


                var parameters = new DynamicParameters();
                parameters.Add("@MenuItemId", model.MenuItemId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@Description", model.Description);
                parameters.Add("@CategoryId", model.CategoryId);
                parameters.Add("@Cost", model.Cost);
                parameters.Add("@SellingPrice", model.SellingPrice);
                parameters.Add("@FeaturedImage", featuredImagePath);
                parameters.Add("@ModifiedBy", actualUserId);
                parameters.Add("@IsActive", true);
                parameters.Add("@MenuItemIngredients", ingredientTable.AsTableValuedParameter("dbo.MenuItemIngredientsType"));
                parameters.Add("@MenuItemRecipes", recipeTable.AsTableValuedParameter("dbo.MenuItemRecipesType"));
                parameters.Add("@Action", "UPDATE");

                int updatedMenuItemId = await connection.ExecuteAsync("sp_MenuCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Menu item updated successfully.";
                ResponseDto.Data = new { MenuItemId = model.MenuItemId };
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

                var results = await connection.QueryAsync<GetMenuItemDTO, string, string, GetMenuItemDTO>(
                    "sp_MenuCRUD",
                    (menu, ingredientsJson, recipesJson) =>
                    {
                        menu.Ingredients = !string.IsNullOrEmpty(ingredientsJson)
                            ? System.Text.Json.JsonSerializer.Deserialize<List<GetMenuItemIngredientDTO>>(ingredientsJson)
                            : new List<GetMenuItemIngredientDTO>();
                        menu.Recipes = !string.IsNullOrEmpty(recipesJson)
                            ? System.Text.Json.JsonSerializer.Deserialize<List<GetMenuItemRecipeDTO>>(recipesJson)
                            : new List<GetMenuItemRecipeDTO>();
                        return menu;
                    },
                    parameters,
                    splitOn: "Ingredients,Recipes",
                    commandType: CommandType.StoredProcedure);

                var menuItem = results.FirstOrDefault();

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Menu item retrieved successfully.";
                ResponseDto.Data = menuItem;
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
        
        public async Task<ResponseDto> GetAllMenuItemsAsync(bool? isActive, int currentUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@IsActive", isActive);
                parameters.Add("@Action", "SELECT_ALL");

                var menuItems = await connection.QueryAsync<GetMenuItemDTO, string, string, GetMenuItemDTO>(
                 "sp_MenuCRUD",
                 (menu, ingredientsJson, recipesJson) =>
                 {
                     menu.Ingredients = !string.IsNullOrEmpty(ingredientsJson)
                         ? System.Text.Json.JsonSerializer.Deserialize<List<GetMenuItemIngredientDTO>>(ingredientsJson)
                         : new List<GetMenuItemIngredientDTO>();

                     menu.Recipes = !string.IsNullOrEmpty(recipesJson)
                         ? System.Text.Json.JsonSerializer.Deserialize<List<GetMenuItemRecipeDTO>>(recipesJson)
                         : new List<GetMenuItemRecipeDTO>();
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

        public async Task<ResponseDto> ToggleMenuActiveAsync(int id, int currentUserId, int actualUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@MenuItemId", id);
                parameters.Add("@ModifiedBy", actualUserId);
                parameters.Add("@Action", "TOGGLE_ACTIVE");

                var result = await connection.QueryFirstAsync("sp_MenuCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = $"Menu Item {(result.IsActive ? "activated" : "deactivated")} successfully.";
                ResponseDto.Data = new 
                { 
                    MenuItemId = id, 
                    IsActive = result.isActive 
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
