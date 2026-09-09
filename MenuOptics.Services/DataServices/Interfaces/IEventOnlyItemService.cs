using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IEventOnlyItemService
    {
        Task<ResponseDto> GetAllAsync(int restaurantId);
        Task<ResponseDto> CreateAsync(EventOnlyItemDto model, int restaurantId);
    }
}
