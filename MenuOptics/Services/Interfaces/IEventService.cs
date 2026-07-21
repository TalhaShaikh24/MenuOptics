using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IEventService
    {
        Task<Response> GetAllEventsAsync(int restaurantId);
        Task<Response> GetEventAsync(int id, int restaurantId);
        Task<Response> CreateEventAsync(Event model, int restaurantId);
        Task<Response> UpdateEventAsync(Event model, int restaurantId);
        Task<Response> DuplicateEventAsync(int id);
        Task<Response> GetEventNoteAsync(int eventId);
        Task<Response> SaveEventNoteAsync(EventNoteDTO model);
        Task<Response> DeleteEventNoteAsync(int eventId);
    }
}
