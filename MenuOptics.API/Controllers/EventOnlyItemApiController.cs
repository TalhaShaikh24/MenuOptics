using MenuOptics.API.Middleware;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [SubscriptionRequired]
    [Authorize(Roles = "Admin,Restaurant")]
    public class EventOnlyItemApiController : ControllerBase
    {
        private readonly IEventOnlyItemService _eventOnlyItemService;

        public EventOnlyItemApiController(IEventOnlyItemService eventOnlyItemService)
        {
            _eventOnlyItemService = eventOnlyItemService;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var items = await _eventOnlyItemService.GetAllAsync(CurrentUserId);
            return Ok(new { data = items });
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] EventOnlyItemDto model)
        {
            var responseData = await _eventOnlyItemService.CreateAsync(model, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }
    }
}
