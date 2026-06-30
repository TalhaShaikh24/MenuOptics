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
    public class ReportApiController : ControllerBase
    {
        private readonly DapperContext _context;

        public ReportApiController(DapperContext context)
        {
            _context = context;
        }

        [HttpPost("CogsReport")]
        public async Task<IActionResult> CogsReport([FromBody] CogsReportRequest model)
        {
            var response = new Response();

            if (!ModelState.IsValid)
            {
                response.Status = 400;
                response.ResponseMsg = "Invalid input.";
                return BadRequest(response);
            }

            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (currentUserId == 0)
                {
                    response.Status = 401;
                    response.ResponseMsg = "Unauthorized";
                    return Unauthorized(response);
                }

                using var connection = _context.CreateConnection();
                connection.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@StartDate", model.StartDate);
                parameters.Add("@EndDate", model.EndDate);

                using var multi = await connection.QueryMultipleAsync("sp_InventorySuiteReport", parameters, commandType: CommandType.StoredProcedure);

                var reportItems = (await multi.ReadAsync<CogsReportItem>()).ToList();
                var totalCogsSummary = await multi.ReadFirstOrDefaultAsync<TotalCogsReportItem>();

                response.Status = 200;
                response.ResponseMsg = "Inventory report generated successfully.";
                response.Data = new
                {
                    Items = reportItems,
                    Summary = totalCogsSummary
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }
        }

    }
}