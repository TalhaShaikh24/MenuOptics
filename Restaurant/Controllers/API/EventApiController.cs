using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Models;
using MenuOptics.Services;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Restaurant")]
    public class EventApiController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventApiController(IEventService eventService)
        {
            _eventService = eventService;
        }

        private int CurrentRestaurantId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpGet("GetAllEvents")]
        public async Task<IActionResult> GetAllEvents()
        {
            var response = await _eventService.GetAllEventsAsync(CurrentRestaurantId);
            return StatusCode(response.Status, response);
        }

        [HttpGet("GetEvent/{id}")]
        public async Task<IActionResult> GetEvent(int id)
        {
            var response = await _eventService.GetEventAsync(id, CurrentRestaurantId);
            return StatusCode(response.Status, response);
        }

        [HttpPost("CreateEvent")]
        public async Task<IActionResult> CreateEvent([FromBody] Event model)
        {
            var response = await _eventService.CreateEventAsync(model, CurrentRestaurantId);
            return StatusCode(response.Status, response);
        }

        [HttpPut("UpdateEvent")]
        public async Task<IActionResult> UpdateEvent([FromBody] Event model)
        {
            var response = await _eventService.UpdateEventAsync(model, CurrentRestaurantId);
            return StatusCode(response.Status, response);
        }

        [HttpPost("DuplicateEvent/{id}")]
        public async Task<IActionResult> DuplicateEvent(int id)
        {
            var response = await _eventService.DuplicateEventAsync(id);
            return StatusCode(response.Status, response);
        }

        [HttpGet("GetEventNote/{eventId}")]
        public async Task<IActionResult> GetEventNote(int eventId)
        {
            var response = await _eventService.GetEventNoteAsync(eventId);
            return StatusCode(response.Status, response);
        }

        [HttpPost("SaveEventNote")]
        public async Task<IActionResult> SaveEventNote([FromBody] EventNoteDTO model)
        {
            var response = await _eventService.SaveEventNoteAsync(model);
            return StatusCode(response.Status, response);
        }

        [HttpDelete("DeleteEventNote/{eventId}")]
        public async Task<IActionResult> DeleteEventNote(int eventId)
        {
            var response = await _eventService.DeleteEventNoteAsync(eventId);
            return StatusCode(response.Status, response);
        }
    }
}
