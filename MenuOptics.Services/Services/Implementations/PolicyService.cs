using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;

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

                var policies = await connection.QueryAsync<PolicyDto>("sp_PolicyCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.Data = policies ?? new List<PolicyDto>();
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

                var policy = await connection.QuerySingleOrDefaultAsync<PolicyDto>("sp_PolicyCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.Data = policy ?? new PolicyDto { PolicyType = type, Content = "" };
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

        public async Task<ResponseDto> SavePolicyAsync(PolicyDto model, int currentUserId)
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
