using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using MenuOptics.Models.DTOs;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Services
{
    public class PrivateVendorService : IPrivateVendorService
    {
        private readonly DapperContext _context;

        public PrivateVendorService(DapperContext context)
        {
            _context = context;
        }

        public async Task<ResponseDto> GetAllPrivateVendorsAsync(int restaurantId, bool? isActive)
        {
            var responseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL");
                parameters.Add("@IsActive", isActive);
                parameters.Add("@RestaurantId", restaurantId);

                var vendors = await connection.QueryAsync<GetPrivateVendorDTO>(
                    "sp_PrivateVendorCRUD", parameters, commandType: CommandType.StoredProcedure);

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Private vendors retrieved successfully.";
                responseDto.Data = vendors;
                return responseDto;
            }
            catch (SqlException sqlEx)
            {
                responseDto.Status = 400;
                responseDto.ResponseMsg = sqlEx.Message;
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = $"{ex.Message}";
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
                parameters.Add("@Action", "SELECT");

                var vendor = await connection.QuerySingleOrDefaultAsync<GetPrivateVendorDTO>(
                    "sp_PrivateVendorCRUD", parameters, commandType: CommandType.StoredProcedure);

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Private vendor retrieved successfully.";
                responseDto.Data = vendor;
                return responseDto;
            }
            catch (SqlException sqlEx)
            {
                responseDto.Status = 400;
                responseDto.ResponseMsg = sqlEx.Message;
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = $"{ex.Message}";
                return responseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> CreatePrivateVendorAsync(CreatePrivateVendorDTO model, int currentUserId, int actualUserId)
        {
            var responseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@CreatedBy", actualUserId);
                parameters.Add("@Action", "INSERT");

                int newId = await connection.ExecuteScalarAsync<int>(
                    "sp_PrivateVendorCRUD", parameters, commandType: CommandType.StoredProcedure);

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Private vendor created successfully.";
                responseDto.Data = new { PrivateVendorId = newId };
                return responseDto;
            }
            catch (SqlException sqlEx)
            {
                responseDto.Status = 400;
                responseDto.ResponseMsg = sqlEx.Message;
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = $"{ex.Message}";
                return responseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> UpdatePrivateVendorAsync(UpdatePrivateVendorDTO model, int currentUserId, int actualUserId)
        {
            var responseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@PrivateVendorId", model.PrivateVendorId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@ModifiedBy", actualUserId);
                parameters.Add("@Action", "UPDATE");

                await connection.ExecuteAsync(
                    "sp_PrivateVendorCRUD", parameters, commandType: CommandType.StoredProcedure);

                responseDto.Status = 200;
                responseDto.ResponseMsg = "Private vendor updated successfully.";
                responseDto.Data = new { PrivateVendorId = model.PrivateVendorId };
                return responseDto;
            }
            catch (SqlException sqlEx)
            {
                responseDto.Status = 400;
                responseDto.ResponseMsg = sqlEx.Message;
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = $"{ex.Message}";
                return responseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> TogglePrivateVendorActiveAsync(int id, int currentUserId, int actualUserId)
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

                var result = await connection.QueryFirstAsync("sp_PrivateVendorCRUD", parameters, commandType: CommandType.StoredProcedure);

                responseDto.Status = 200;
                responseDto.ResponseMsg = $"Private Vendor {(result.IsActive ? "activated" : "deactivated")} successfully."; ;
                responseDto.Data = new 
                { 
                    PrivateVendorId = id, 
                    IsActive = result.IsActive 
                };

                return responseDto;
            }
            catch (SqlException sqlEx)
            {
                responseDto.Status = 400;
                responseDto.ResponseMsg = sqlEx.Message;
                return responseDto;
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = $"{ex.Message}";
                return responseDto;
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
