using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IInventoryService
    {
        Task<(int StatusCode, object Body)> AddIngredientStockAsync(Inventory model, int currentUserId, int actualUserId);
        Task<ResponseDto> UpdateClosingCountAsync(ClosingCountUpdateDto model, int currentUserId);
    }
}
