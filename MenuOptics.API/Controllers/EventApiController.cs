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
            var responseData = await _eventService.GetAllEventsAsync(CurrentRestaurantId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpGet("GetEvent/{id}")]
        public async Task<IActionResult> GetEvent(int id)
        {
            var responseData = await _eventService.GetEventAsync(id, CurrentRestaurantId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPost("CreateEvent")]
        public async Task<IActionResult> CreateEvent([FromBody] Event model)
        {
            var responseData = await _eventService.CreateEventAsync(model, CurrentRestaurantId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPut("UpdateEvent")]
        public async Task<IActionResult> UpdateEvent([FromBody] Event model)
        {
            var responseData = await _eventService.UpdateEventAsync(model, CurrentRestaurantId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPost("DuplicateEvent/{id}")]
        public async Task<IActionResult> DuplicateEvent(int id)
        {
            var responseData = await _eventService.DuplicateEventAsync(id);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpGet("GetEventNote/{eventId}")]
        public async Task<IActionResult> GetEventNote(int eventId)
        {
            var responseData = await _eventService.GetEventNoteAsync(eventId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPost("SaveEventNote")]
        public async Task<IActionResult> SaveEventNote([FromBody] EventNoteDto model)
        {
            var responseData = await _eventService.SaveEventNoteAsync(model);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpDelete("DeleteEventNote/{eventId}")]
        public async Task<IActionResult> DeleteEventNote(int eventId)
        {
            var responseData = await _eventService.DeleteEventNoteAsync(eventId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPut("ToggleEventActive/{id}")]
        public async Task<IActionResult> ToggleEventActive(int id)
        {
            var responseData = await _eventService.ToggleEventActiveAsync(id, CurrentRestaurantId);
            return StatusCode(responseData.Status, responseData);
        }
    }
}
