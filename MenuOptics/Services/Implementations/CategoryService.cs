using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;

namespace MenuOptics.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly DapperContext _context;

        public CategoryService(DapperContext context)
        {
            _context = context;
        }

        public async Task<Response> GetAllCategoriesAsync(int restaurantId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL");
                parameters.Add("@RestaurantId", restaurantId);

                var categories = await connection.QueryAsync<Category>("sp_CategoryCRUD", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Categories retrieved successfully.";
                response.Data = categories;
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

        public async Task<Response> GetCategoryAsync(int id, int restaurantId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CategoryId", id);
                parameters.Add("@RestaurantId", restaurantId);
                parameters.Add("@Action", "SELECT_BY_ID");

                var category = await connection.QuerySingleOrDefaultAsync<Category>("sp_CategoryCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (category == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Category not found.";
                    return response;
                }

                response.Status = 200;
                response.ResponseMsg = "Category retrieved successfully.";
                response.Data = category;
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

        public async Task<Response> CreateCategoryAsync(Category model, int currentUserId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Name", model.Name);
                parameters.Add("@CreatedBy", currentUserId);
                parameters.Add("@Action", "INSERT");

                int categoryId = await connection.ExecuteScalarAsync<int>("sp_CategoryCRUD", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Category created successfully.";
                response.Data = new { CategoryId = categoryId };
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

        public async Task<Response> UpdateCategoryAsync(Category model, int currentUserId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CategoryId", model.CategoryId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@IsActive", model.IsActive);
                parameters.Add("@Action", "UPDATE");

                await connection.ExecuteAsync("sp_CategoryCRUD", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Category updated successfully.";
                response.Data = new { CategoryId = model.CategoryId };
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

        public async Task<Response> ToggleCategoryActiveAsync(int id, int currentUserId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CategoryId", id);
                parameters.Add("@Action", "SELECT_BY_ID");

                var category = await connection.QueryFirstOrDefaultAsync<Category>("sp_CategoryCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (category == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Category not found.";
                    return response;
                }

                parameters = new DynamicParameters();
                parameters.Add("@CategoryId", id);
                parameters.Add("@IsActive", !category.IsActive);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "TOGGLE_ACTIVE");

                await connection.ExecuteAsync("sp_CategoryCRUD", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = $"Category {(category.IsActive ? "deactivated" : "activated")} successfully.";
                response.Data = new { CategoryId = id, IsActive = !category.IsActive };
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

        public async Task<Response> DeleteCategoryByIdAsync(int id, int currentUserId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CategoryId", id);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "DELETE");

                await connection.ExecuteAsync("sp_CategoryCRUD", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Category deleted successfully.";
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
