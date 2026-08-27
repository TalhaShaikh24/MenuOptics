using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IEventService
    {
        Task<ResponseDto> GetAllEventsAsync(int currentUserId);
        Task<ResponseDto> GetEventAsync(int id, int currentUserId);
        Task<ResponseDto> CreateEventAsync(Event model, int currentUserId, int actualUserId);
        Task<ResponseDto> UpdateEventAsync(Event model, int currentUserId, int actualUserId);
        Task<ResponseDto> DuplicateEventAsync(int id);
        Task<ResponseDto> GetEventNoteAsync(int eventId);
        Task<ResponseDto> SaveEventNoteAsync(EventNoteDto model);
        Task<ResponseDto> DeleteEventNoteAsync(int eventId);
        Task<ResponseDto> ToggleEventActiveAsync(int id, int currentUserId, int actualUserId);
    }
}
