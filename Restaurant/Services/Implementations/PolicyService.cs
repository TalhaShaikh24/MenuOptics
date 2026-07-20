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

        public async Task<Response> GetAllPoliciesAsync()
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL");

                var policies = await connection.QueryAsync<PolicyDTO>("sp_PolicyCRUD", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.Data = policies ?? new List<PolicyDTO>();
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> GetPolicyByTypeAsync(string type)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@PolicyType", type);
                parameters.Add("@Action", "SELECT_BY_TYPE");

                var policy = await connection.QuerySingleOrDefaultAsync<PolicyDTO>("sp_PolicyCRUD", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.Data = policy ?? new PolicyDTO { PolicyType = type, Content = "" };
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> SavePolicyAsync(PolicyDTO model, int currentUserId)
        {
            var response = new Response();
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

                response.Status = 200;
                response.ResponseMsg = $"{model.PolicyType} saved successfully.";
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
