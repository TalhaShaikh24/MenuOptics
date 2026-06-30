using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Restaurant")]
    public class EventOnlyItemApiController : ControllerBase
    {
        private readonly DapperContext _context;

        public EventOnlyItemApiController(DapperContext context)
        {
            _context = context;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            using var connection = _context.CreateConnection();
            var restaurantId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var items = await connection.QueryAsync<EventOnlyItemDTO>(
                "sp_EventOnlyItemCRUD",
                new { Action = "SELECT_ALL", RestaurantId = restaurantId },
                commandType: CommandType.StoredProcedure
            );
            return Ok(new { data = items });
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] EventOnlyItemDTO model)
        {
            using var connection = _context.CreateConnection();
            var restaurantId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var newId = await connection.ExecuteScalarAsync<int>(
                "sp_EventOnlyItemCRUD",
                new { Action = "INSERT", RestaurantId = restaurantId, Name = model.Name, DefaultCost = model.DefaultCost },
                commandType: CommandType.StoredProcedure
            );

            return Ok(new { status = 200, data = new { EventOnlyItemId = newId, Name = model.Name, DefaultCost = model.DefaultCost } });
        }
    }
}