using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IInventoryService
    {
        Task<ResponseDto> GetInventoryAsync(int id);
        Task<ResponseDto> GetAllInventoryByRestaurantAsync(int restaurantId, int currentUserId);
        Task<ResponseDto> UpdateClosingCountAsync(ClosingCountUpdateDto model, int currentUserId);
    }
}
