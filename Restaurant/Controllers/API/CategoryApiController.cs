using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Restaurant")]
    public class CategoryApiController : ControllerBase
    {
        private readonly DapperContext _context;

        public CategoryApiController(DapperContext context)
        {
            _context = context;
        }

        [HttpGet("GetAllCategories")]
        public async Task<IActionResult> GetAllCategories()
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL");
                parameters.Add("@RestaurantId", currentUserId);

                var categories = await connection.QueryAsync<Category>("sp_CategoryCRUD", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Categories retrieved successfully.";
                response.Data = categories;
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

        [HttpGet("GetCategory/{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var parameters = new DynamicParameters();
                parameters.Add("@CategoryId", id);
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@Action", "SELECT_BY_ID");

                var category = await connection.QuerySingleOrDefaultAsync<Category>("sp_CategoryCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (category == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Category not found.";
                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "Category retrieved successfully.";
                response.Data = category;
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

        [HttpPost("CreateCategory")]
        public async Task<IActionResult> CreateCategory([FromBody] Category model)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var parameters = new DynamicParameters();
                parameters.Add("@Name", model.Name);
                parameters.Add("@CreatedBy", currentUserId);
                parameters.Add("@Action", "INSERT");

                int categoryId = await connection.ExecuteScalarAsync<int>("sp_CategoryCRUD", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Category created successfully.";
                response.Data = new { CategoryId = categoryId };
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

        [HttpPut("UpdateCategory")]
        public async Task<IActionResult> UpdateCategory([FromBody] Category model)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

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

     
        [HttpPut("ToggleCategoryActive/{id}")]
        public async Task<IActionResult> ToggleCategoryActive(int id)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var parameters = new DynamicParameters();
                parameters.Add("@CategoryId", id);
                parameters.Add("@Action", "SELECT_BY_ID");

                var category = await connection.QueryFirstOrDefaultAsync<Category>("sp_CategoryCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (category == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Category not found.";
                    return NotFound(response);
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

        [HttpDelete("DeleteCategoryById/{id}")]
        public async Task<IActionResult> DeleteCategoryById(int id)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var parameters = new DynamicParameters();
                parameters.Add("@CategoryId", id);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "DELETE");

                await connection.ExecuteAsync("sp_CategoryCRUD", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Category deleted successfully.";
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