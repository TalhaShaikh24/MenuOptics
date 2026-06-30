using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Restaurant.Data;
using Restaurant.Models;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Restaurant.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class IngredientApiController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public IngredientApiController(DapperContext context, IWebHostEnvironment environment, IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _configuration = configuration;
        }

        [HttpPost("CreateIngredientByVendor")]
        // [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> CreateIngredientByVendor([FromBody] Ingredient model)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", currentUserId); // Using authenticated user's ID as UserId
                parameters.Add("@VendorId", model.VendorId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@Price", model.Price);
                parameters.Add("@Unit", model.Unit);
                parameters.Add("@CreatedBy", currentUserId); // Using authenticated user's ID as CreatedBy
                parameters.Add("@Action", "INSERT");

                int ingredientId = await connection.ExecuteScalarAsync<int>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);
                response.Status = 200;
                response.ResponseMsg = "Ingredient created successfully.";
                response.Data = new { IngredientId = ingredientId };
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

        [HttpPost("CreateIngredient")]
        //// [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> CreateIngredient([FromBody] Ingredient model)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", currentUserId); // Using authenticated user's ID as UserId
                parameters.Add("@Name", model.Name);
                parameters.Add("@Price", model.Price);
                parameters.Add("@Unit", model.Unit);
                parameters.Add("@CreatedBy", currentUserId); // Using authenticated user's ID as CreatedBy
                parameters.Add("@Action", "INSERT");

                int ingredientId = await connection.ExecuteScalarAsync<int>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);
                response.Status = 200;
                response.ResponseMsg = "Ingredient created successfully.";
                response.Data = new { IngredientId = ingredientId };
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

        [HttpGet("GetIngredient/{id}")]
        // [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> GetIngredient(int id)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@IngredientId", id);
                parameters.Add("@Action", "SELECT");

                var ingredient = await connection.QueryFirstOrDefaultAsync<IngredientDTO>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (ingredient == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Ingredient not found.";
                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "Ingredient retrieved successfully.";
                response.Data = ingredient;
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

        [HttpGet("GetAllIngredients")]
        // [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> GetAllIngredients()
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL");

                var ingredients = await connection.QueryAsync<IngredientDTO>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);
                response.Status = 200;
                response.ResponseMsg = "Ingredients retrieved successfully.";
                response.Data = ingredients;
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

        [HttpGet("GetAllIngredientsByUserId")]
        public async Task<IActionResult> GetAllIngredientsByUserId()
        {

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1");

            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@Action", "SELECT_ALL_BY_USERID");

                var ingredients = await connection.QueryAsync<IngredientDTO>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);
                response.Status = 200;
                response.ResponseMsg = "Ingredients retrieved successfully.";
                response.Data = ingredients;
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

        [HttpGet("GetAllIngredientsByVendorId/{vendorId}")]
        // [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> GetAllIngredientsByVendorId(int vendorId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL"); // Using SELECT_ALL and filtering in code
                var ingredients = (await connection.QueryAsync<IngredientDTO>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure))
                    .Where(i => i.VendorId == vendorId).ToList();

                if (!ingredients.Any())
                {
                    response.Status = 404;
                    response.ResponseMsg = "No ingredients found for the specified vendor.";
                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "Ingredients retrieved successfully.";
                response.Data = ingredients;
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

        [HttpGet("GetAllIngredientsByUserId/{userId}")]
        // [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> GetAllIngredientsByUserId(int userId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL"); // Using SELECT_ALL and filtering in code
                var ingredients = (await connection.QueryAsync<IngredientDTO>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure))
                    .Where(i => i.UserId == userId).ToList();

                if (!ingredients.Any())
                {
                    response.Status = 404;
                    response.ResponseMsg = "No ingredients found for the specified user.";
                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "Ingredients retrieved successfully.";
                response.Data = ingredients;
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

        [HttpPut("UpdateIngredient")]
        // [Authorize(Roles = "Vendor")] // Uncomment if authorization is needed
        public async Task<IActionResult> UpdateIngredient([FromBody] Ingredient model)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                // Ensure IngredientId is provided for update
                if (model.IngredientId <= 0)
                {
                    response.Status = 400;
                    response.ResponseMsg = "IngredientId is required for updating an ingredient.";
                    return BadRequest(response);
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var parameters = new DynamicParameters();
                parameters.Add("@IngredientId", model.IngredientId);
                parameters.Add("@UserId", currentUserId); // Assuming UserId can be updated or is part of the model
                parameters.Add("@Name", model.Name);
                parameters.Add("@Price", model.Price);
                parameters.Add("@Unit", model.Unit);
                parameters.Add("@ModifiedBy", currentUserId); // Set ModifiedBy to the current authenticated user
                parameters.Add("@IsActive", 1); // Assuming IsActive can be updated
                parameters.Add("@Action", "UPDATE");

                int affectedRows = await connection.ExecuteAsync("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (affectedRows == 0)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Ingredient not found or no changes were made.";
                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "Ingredient updated successfully.";
                response.Data = new { IngredientId = model.IngredientId };
                return Ok(response);
            }
            catch (SqlException sqlEx)
            {
                // SQL Server specific exceptions
                response.Status = 503;
                response.ResponseMsg = $"Expection: {sqlEx.Message}";
                return StatusCode(503, response);
            }
            catch (InvalidOperationException invalidOpEx)
            {
                response.Status = 422;
                response.ResponseMsg = $"Invalid operation: {invalidOpEx.Message}";
                return StatusCode(422, response);
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

        [HttpDelete("DeleteIngredientById/{id}")]
        // [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> DeleteIngredientById(int id)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@IngredientId", id);
                parameters.Add("@Action", "DELETE");

                var affectedRows = await connection.ExecuteAsync("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);
                if (affectedRows == 0)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Ingredient not found.";
                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "Ingredient deleted successfully.";
                response.Data = new { IngredientId = id };
                return Ok(response);
            }
            catch (SqlException sqlEx)
            {
                // SQL Server specific exceptions
                response.Status = 503;
                response.ResponseMsg = $"Expection: {sqlEx.Message}";
                return StatusCode(503, response);
            }
            catch (InvalidOperationException invalidOpEx)
            {
                response.Status = 422;
                response.ResponseMsg = $"Invalid operation: {invalidOpEx.Message}";
                return StatusCode(422, response);
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



        [HttpGet("GetAllVendors")]
        public async Task<IActionResult> GetAllVendors()
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1");
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", currentUserId);

                var vendors = await connection.QueryAsync<User>(@"
                    SELECT 
                        u.UserId, 
                        u.FirstName, 
                        u.LastName, 
                        u.Username, 
                        u.Email, 
                        u.Phone, 
                        u.ProfilePic, 
                        u.Address, 
                        u.Country, 
                        u.State, 
                        u.City, 
                        u.Zip, 
                        u.Gender,
                        u.RestaurantName,
                        u.CuisineType,
                        u.RestaurantSize,
                        u.IsActive,
                        STRING_AGG(r.RoleName, ',') AS Roles,
                        u.CreatedOn, 
                        u.ModifiedOn,
                        CASE 
                            WHEN l.LinkedVendorId IS NOT NULL THEN 1 
                            ELSE 0 
                        END AS IsLinked
                    FROM Users u
                    INNER JOIN UserRoles ur ON u.UserId = ur.UserId
                    INNER JOIN Roles r ON ur.RoleId = r.RoleId
                    LEFT JOIN UserVendorIngredientsLinks l 
                        ON l.LinkedVendorId = u.UserId AND l.UserId = @UserId
                    WHERE r.RoleName = 'Vendor'
                    GROUP BY 
                        u.UserId, u.FirstName, u.LastName, u.Username, u.Email, u.Phone, u.ProfilePic, 
                        u.Address, u.Country, u.State, u.City, u.Zip, u.Gender, u.RestaurantName, 
                        u.CuisineType, u.RestaurantSize, u.IsActive, u.CreatedOn, u.ModifiedOn, 
                        l.LinkedVendorId",
                    parameters);

                response.Status = 200;
                response.ResponseMsg = "Vendors retrieved successfully.";
                response.Data = vendors;
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

        [HttpPost("LinkVendor")]
        public async Task<IActionResult> LinkVendor([FromBody] User model)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@VendorId", model.UserId);
                parameters.Add("@Action", "INSERT");

                var affectedRows = await connection.ExecuteScalarAsync<int>("sp_UserVendorLinkCRUD", parameters, commandType: CommandType.StoredProcedure);
                if (affectedRows == 0)
                {
                    response.Status = 400;
                    response.ResponseMsg = "Vendor is already linked.";
                    return BadRequest(response);
                }

                response.Status = 200;
                response.ResponseMsg = "Vendor linked successfully.";
                response.Data = new { VendorId = model.UserId };
                return Ok(response);
            }
            catch (SqlException sqlEx)
            {
                response.Status = 503;
                response.ResponseMsg = $"Database error: {sqlEx.Message}";
                return StatusCode(503, response);
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

        [HttpDelete("UnlinkVendor/{vendorId}")]
        public async Task<IActionResult> UnlinkVendor(int vendorId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@VendorId", vendorId);
                parameters.Add("@Action", "DELETE");

                var affectedRows = await connection.ExecuteScalarAsync<int>("sp_UserVendorLinkCRUD", parameters, commandType: CommandType.StoredProcedure);
                if (affectedRows == 0)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Vendor link not found.";
                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "Vendor unlinked successfully.";
                response.Data = new { VendorId = vendorId };
                return Ok(response);
            }
            catch (SqlException sqlEx)
            {
                response.Status = 503;
                response.ResponseMsg = $"Database error: {sqlEx.Message}";
                return StatusCode(503, response);
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