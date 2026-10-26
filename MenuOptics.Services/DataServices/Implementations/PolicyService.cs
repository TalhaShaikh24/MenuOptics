using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Services
{
    public class PolicyService : IPolicyService
    {
        private readonly DapperContext _context;

        public PolicyService(DapperContext context)
        {
            _context = context;
        }

        public async Task<ResponseDto> GetAllPoliciesAsync()
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL");

                var policies = await connection.QueryAsync<GetPolicyDTO>("sp_PolicyCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.Data = policies ?? new List<GetPolicyDTO>();
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

        public async Task<ResponseDto> GetPolicyByTypeAsync(string type)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@PolicyType", type);
                parameters.Add("@Action", "SELECT_BY_TYPE");

                var policy = await connection.QuerySingleOrDefaultAsync<GetPolicyDTO>("sp_PolicyCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.Data = policy ?? new GetPolicyDTO { PolicyType = type, Content = "" };
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

        public async Task<ResponseDto> SavePolicyAsync(SavePolicyDTO model, int currentUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@PolicyType", model.PolicyType);
                parameters.Add("@Content", model.Content);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "UPSERT");

                await connection.ExecuteAsync("sp_PolicyCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = $"{model.PolicyType} saved successfully.";
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
