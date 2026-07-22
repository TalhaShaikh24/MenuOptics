using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;

namespace MenuOptics.Services
{
    public class VendorService : IVendorService
    {
        private readonly DapperContext _context;
        private readonly IWebHostEnvironment _environment;

        public VendorService(DapperContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<ResponseDto> RegisterVendorAsync(User model, string requestBaseUrl)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var filePathUrl = "";
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
                    filePathUrl = $"{requestBaseUrl}/Uploads/{fileName}";
                }

                var parameters = new DynamicParameters();
                parameters.Add("@FirstName", model.FirstName);
                parameters.Add("@LastName", model.LastName);
                parameters.Add("@Username", model.Username);
                parameters.Add("@Email", model.Email);
                parameters.Add("@Phone", model.Phone);
                parameters.Add("@ProfilePic", filePathUrl);
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
                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Vendor registered successfully. Awaiting admin approval.";
                ResponseDto.Data = new { UserId = userId };
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
    }
}
