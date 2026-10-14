using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly DapperContext _context;

        public CategoryService(DapperContext context)
        {
            _context = context;
        }

        public async Task<ResponseDto> GetAllCategoriesAsync(int restaurantId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL");
                parameters.Add("@RestaurantId", restaurantId);

                var categories = await connection.QueryAsync<Category>("sp_CategoryCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Categories retrieved successfully.";
                ResponseDto.Data = categories;
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

        public async Task<ResponseDto> GetCategoryAsync(int id, int restaurantId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CategoryId", id);
                parameters.Add("@Action", "SELECT");

                var category = await connection.QuerySingleOrDefaultAsync<Category>("sp_CategoryCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Category retrieved successfully.";
                ResponseDto.Data = category;
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

        public async Task<ResponseDto> CreateCategoryAsync(Category model, int currentUserId, int actualUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();

                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@CreatedBy", actualUserId);
                parameters.Add("@Action", "INSERT");

                int categoryId = await connection.ExecuteScalarAsync<int>("sp_CategoryCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Category created successfully.";
                ResponseDto.Data = new { CategoryId = categoryId };
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

        public async Task<ResponseDto> UpdateCategoryAsync(Category model, int currentUserId, int actualUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CategoryId", model.CategoryId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@ModifiedBy", actualUserId);
                parameters.Add("@IsActive", model.IsActive);
                parameters.Add("@Action", "UPDATE");

                await connection.ExecuteAsync("sp_CategoryCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Category updated successfully.";
                ResponseDto.Data = new { CategoryId = model.CategoryId };
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

        public async Task<ResponseDto> ToggleCategoryActiveAsync(int id, int currentUserId, int actualUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();

                parameters = new DynamicParameters();
                parameters.Add("@CategoryId", id);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "TOGGLE_ACTIVE");

                var result = await connection.QueryFirstAsync("sp_CategoryCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = $"Category {(result.IsActive ? "activated" : "deactivated")} successfully.";
                ResponseDto.Data = new { CategoryId = id, IsActive = result.IsActive };
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
