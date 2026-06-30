using Dapper;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Data;
using Restaurant.Models;
using System.Data;
using System.Threading.Tasks;

namespace Restaurant.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuEngineeringInsightsApiController : ControllerBase
    {
        private readonly DapperContext _context;

        public MenuEngineeringInsightsApiController(DapperContext context)
        {
            _context = context;
        }

        [HttpGet("GetAllMenuEngineeringInsightsByUserId")]
        public async Task<IActionResult> GetAllMenuEngineeringInsightsByUserId()
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", currentUserId);

                var insights = await connection.QueryAsync<MenuEngineeringInsights>("sp_GetMenuEngineeringInsightsByUserId", parameters, commandType: CommandType.StoredProcedure);

                if (!insights.Any())
                {
                    response.Status = 404;
                    response.ResponseMsg = "No menu engineering insights found for the specified user.";
                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "Menu engineering insights retrieved successfully.";
                response.Data = insights;
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