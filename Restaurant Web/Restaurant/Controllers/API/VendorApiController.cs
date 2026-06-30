using Dapper;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Data;
using Restaurant.Models;
using System.Data;

namespace Restaurant.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class VendorApiController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public VendorApiController(DapperContext context, IWebHostEnvironment environment, IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _configuration = configuration;
        }

        [HttpPost("RegisterVendor")]
        public async Task<IActionResult> RegisterVendor([FromForm] User model)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var FilePathUrl = "";
                if (model.File != null)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath ?? Directory.GetCurrentDirectory(), "Uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.File.FileName);
                    string filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.File.CopyToAsync(stream);
                    }
                    FilePathUrl = $"{Request.Scheme}://{Request.Host}/Uploads/{fileName}";
                }

                var parameters = new DynamicParameters();
                parameters.Add("@FirstName", model.FirstName);
                parameters.Add("@LastName", model.LastName);
                parameters.Add("@Username", model.Username);
                parameters.Add("@Email", model.Email);
                parameters.Add("@Phone", model.Phone);
                parameters.Add("@ProfilePic", FilePathUrl);
                parameters.Add("@Address", model.Address);
                parameters.Add("@Country", model.Country);
                parameters.Add("@State", model.State);
                parameters.Add("@City", model.City);
                parameters.Add("@Zip", model.Zip);
                parameters.Add("@Gender", model.Gender);
                parameters.Add("@RestaurantName", model.RestaurantName);
                parameters.Add("@CuisineType", model.CuisineType);
                parameters.Add("@RestaurantSize", model.RestaurantSize);
                parameters.Add("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(model.Password));
                parameters.Add("@RoleName", "Vendor");
                parameters.Add("@CreatedBy", model.CreatedBy ?? 1);
                parameters.Add("@IsActive", 0); // Vendors are inactive by default
                parameters.Add("@Action", "INSERT");

                int userId = await connection.ExecuteScalarAsync<int>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                response.Status = 200;
                response.ResponseMsg = "Vendor registered successfully. Awaiting admin approval.";
                response.Data = new { UserId = userId };
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