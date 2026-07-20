using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IEventOnlyItemService
    {
        Task<IEnumerable<EventOnlyItemDTO>> GetAllAsync(int restaurantId);
        Task<int> CreateAsync(EventOnlyItemDTO model, int restaurantId);
    }
}
