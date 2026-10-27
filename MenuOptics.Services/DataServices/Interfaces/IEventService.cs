using MenuOptics.Models;
using MenuOptics.Models.DTOs;

namespace MenuOptics.Services
{
    public interface IEventService
    {
        Task<ResponseDto> GetAllEventsAsync(int currentUserId);
        Task<ResponseDto> GetEventAsync(int id, int currentUserId);
        Task<ResponseDto> CreateEventAsync(CreateEventDTO model, int currentUserId, int actualUserId);
        Task<ResponseDto> UpdateEventAsync(UpdateEventDTO model, int currentUserId, int actualUserId);
        Task<ResponseDto> DuplicateEventAsync(int id);
        Task<ResponseDto> ToggleEventActiveAsync(int id, int currentUserId, int actualUserId);
    }
}
