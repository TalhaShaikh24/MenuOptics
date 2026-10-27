using MenuOptics.Models;
using MenuOptics.Models.DTOs;

namespace MenuOptics.Services
{
    public interface IEventOnlyItemService
    {
        Task<ResponseDto> GetAllAsync(int restaurantId);
        Task<ResponseDto> CreateAsync(CreateEventOnlyItemDTO model, int restaurantId);
    }
}
