using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IEventService
    {
        Task<ResponseDto> GetAllEventsAsync(int restaurantId);
        Task<ResponseDto> GetEventAsync(int id, int restaurantId);
        Task<ResponseDto> CreateEventAsync(Event model, int restaurantId);
        Task<ResponseDto> UpdateEventAsync(Event model, int restaurantId);
        Task<ResponseDto> DuplicateEventAsync(int id);
        Task<ResponseDto> GetEventNoteAsync(int eventId);
        Task<ResponseDto> SaveEventNoteAsync(EventNoteDto model);
        Task<ResponseDto> DeleteEventNoteAsync(int eventId);
        Task<ResponseDto> ToggleEventActiveAsync(int id, int restaurantId);
    }
}
