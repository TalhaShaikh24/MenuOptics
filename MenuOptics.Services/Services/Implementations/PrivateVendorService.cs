using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;

namespace MenuOptics.Services
{
    public class PrivateVendorService : IPrivateVendorService
    {
        private readonly DapperContext _context;

        public PrivateVendorService(DapperContext context)
        {
            _context = context;
        }

        public async Task<ResponseDto> GetAllPrivateVendorsAsync(int restaurantId)
        {
            var responseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL");
                parameters.Add("@RestaurantId", restaurantId);

                var vendors = await connection.QueryAsync<PrivateVendor>(
                    "sp_PrivateVendorCRUD", parameters, commandType: CommandType.StoredProcedure);

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Private vendors retrieved successfully.";
                responseDto.Data = vendors;
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = $"Server error: {ex.Message}";
                return responseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> GetPrivateVendorAsync(int id, int restaurantId)
        {
            var responseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@PrivateVendorId", id);
                parameters.Add("@RestaurantId", restaurantId);
                parameters.Add("@Action", "SELECT_BY_ID");

                var vendor = await connection.QuerySingleOrDefaultAsync<PrivateVendor>(
                    "sp_PrivateVendorCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (vendor == null)
                {
                    responseDto.Status = 404;
                    responseDto.ResponseMsg = "Private vendor not found.";
                    return responseDto;
                }

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Private vendor retrieved successfully.";
                responseDto.Data = vendor;
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = $"Server error: {ex.Message}";
                return responseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> CreatePrivateVendorAsync(PrivateVendor model, int currentUserId)
        {
            var responseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Name", model.Name);
                parameters.Add("@CreatedBy", currentUserId);
                parameters.Add("@IsActive", true);
                parameters.Add("@Action", "INSERT");

                int newId = await connection.ExecuteScalarAsync<int>(
                    "sp_PrivateVendorCRUD", parameters, commandType: CommandType.StoredProcedure);

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Private vendor created successfully.";
                responseDto.Data = new { PrivateVendorId = newId };
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = $"Server error: {ex.Message}";
                return responseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> UpdatePrivateVendorAsync(PrivateVendor model, int currentUserId)
        {
            var responseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@PrivateVendorId", model.PrivateVendorId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@IsActive", model.IsActive);
                parameters.Add("@Action", "UPDATE");

                await connection.ExecuteAsync(
                    "sp_PrivateVendorCRUD", parameters, commandType: CommandType.StoredProcedure);

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Private vendor updated successfully.";
                responseDto.Data = new { PrivateVendorId = model.PrivateVendorId };
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = $"Server error: {ex.Message}";
                return responseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> TogglePrivateVendorActiveAsync(int id, int currentUserId)
        {
            var responseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@PrivateVendorId", id);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "TOGGLE_ACTIVE");

                await connection.ExecuteAsync(
                    "sp_PrivateVendorCRUD", parameters, commandType: CommandType.StoredProcedure);

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Private vendor status toggled successfully.";
                responseDto.Data = new { PrivateVendorId = id };
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = $"Server error: {ex.Message}";
                return responseDto;
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
