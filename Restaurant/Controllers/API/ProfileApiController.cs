using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Data;
using MenuOptics.Models;
using Dapper;
using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class ProfileApiController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProfileApiController(DapperContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet("GetCurrentUser")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var response = new Response();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            using var connection = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);
            parameters.Add("@Action", "SELECT");

            var user = await connection.QueryFirstOrDefaultAsync<UserDTO>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

            if (user == null) return NotFound(new { responseMsg = "User not found" });

            response.Status = 200;
            response.Data = user;
            return Ok(response);
        }

        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromForm] User model)
        {
            var response = new Response();
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            using var connection = _context.CreateConnection();
            try
            {
                string filePathUrl = model.ProfilePic;

                if (model.File != null)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "Uploads");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.File.FileName);
                    string filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.File.CopyToAsync(stream);
                    }
                    filePathUrl = $"{Request.Scheme}://{Request.Host}/Uploads/{fileName}";
                }

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@FirstName", model.FirstName);
                parameters.Add("@LastName", model.LastName);
                parameters.Add("@Phone", model.Phone);
                parameters.Add("@ProfilePic", filePathUrl);
                parameters.Add("@Address", model.Address);
                parameters.Add("@Country", model.Country);
                parameters.Add("@State", model.State);
                parameters.Add("@City", model.City);
                parameters.Add("@Zip", model.Zip);
                parameters.Add("@Gender", model.Gender);
                // Profile update mein hum usually Role aur IsActive change nahi karne dete user ko
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "UPDATE");

                var updatedUser = await connection.QueryFirstOrDefaultAsync<User>(
     "sp_UserCRUD",
     parameters,
     commandType: CommandType.StoredProcedure
 );


                return Ok(new
                {
                    status = 200,
                    responseMsg = "Profile updated successfully!",
                    data = updatedUser
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 500, responseMsg = ex.Message });
            }
        }
    }
}