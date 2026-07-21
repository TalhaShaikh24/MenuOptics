using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;

namespace MenuOptics.Services
{
    public class ProfileService : IProfileService
    {
        private readonly DapperContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProfileService(DapperContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<(int StatusCode, object Body)> GetCurrentUserAsync(string? userId)
        {
            var response = new Response();

            using var connection = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);
            parameters.Add("@Action", "SELECT");

            var user = await connection.QueryFirstOrDefaultAsync<UserDTO>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

            if (user == null) return (404, new { responseMsg = "User not found" });

            response.Status = 200;
            response.Data = user;
            return (200, response);
        }

        public async Task<(int StatusCode, object Body)> UpdateProfileAsync(User model, int currentUserId, string requestBaseUrl)
        {
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
                    filePathUrl = $"{requestBaseUrl}/Uploads/{fileName}";
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
                // Role and IsActive are intentionally not updatable from the profile screen.
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "UPDATE");

                var updatedUser = await connection.QueryFirstOrDefaultAsync<User>(
                    "sp_UserCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return (200, new
                {
                    status = 200,
                    responseMsg = "Profile updated successfully!",
                    data = updatedUser
                });
            }
            catch (Exception ex)
            {
                return (500, new { status = 500, responseMsg = ex.Message });
            }
        }
    }
}
