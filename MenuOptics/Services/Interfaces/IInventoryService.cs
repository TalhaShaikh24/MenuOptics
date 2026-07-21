using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IInventoryService
    {
        Task<Response> InvoiceInventoryAsync(InvoiceInventoryRequest model, int currentUserId, string requestId);
        Task<Response> GetInventoryAsync(int id);
        Task<Response> GetAllInventoryByRestaurantAsync(int restaurantId, int currentUserId);
        Task<Response> UpdateClosingCountAsync(ClosingCountUpdateDto model, int currentUserId);
    }
}
