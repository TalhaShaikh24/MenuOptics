using Dapper;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class PolicyApiController : ControllerBase
    {
        private readonly DapperContext _context;

        public PolicyApiController(DapperContext context)
        {
            _context = context;
        }

        // Add new endpoint to get ALL policies at once
        [HttpGet("GetAllPolicies")]
        public async Task<IActionResult> GetAllPolicies()
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL");

                // Assuming sp_PolicyCRUD can handle SELECT_ALL action
                var policies = await connection.QueryAsync<PolicyDTO>("sp_PolicyCRUD", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.Data = policies ?? new List<PolicyDTO>();
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

        [HttpGet("GetPolicyByType")]
        public async Task<IActionResult> GetPolicyByType(string type)
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

        [HttpPost("SavePolicy")]
        public async Task<IActionResult> SavePolicy([FromBody] PolicyDTO model)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var parameters = new DynamicParameters();
                parameters.Add("@PolicyType", model.PolicyType);
                parameters.Add("@Content", model.Content);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "UPSERT");

                await connection.ExecuteAsync("sp_PolicyCRUD", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = $"{model.PolicyType} saved successfully.";
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