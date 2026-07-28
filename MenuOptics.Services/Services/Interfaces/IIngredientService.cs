using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IIngredientService
    {
        Task<(int StatusCode, object Body)> CreateIngredientByVendorAsync(VendorIngredientDto model, int currentUserId, string requestId);
        Task<(int StatusCode, object Body)> UpdateIngredientPricesAsync(UpdateIngredientPriceDto model, int currentUserId, string requestId);
        Task<(int StatusCode, object Body)> CreateIngredientAsync(Ingredient model, int currentUserId);
        Task<(int StatusCode, object Body)> AddIngredientStockAsync(Inventory model, int currentUserId);
        Task<(int StatusCode, object Body)> GetIngredientAsync(int id);
        Task<(int StatusCode, object Body)> GetAllIngredientsAsync(IQueryCollection query);
        Task<(int StatusCode, object Body)> GetAllActiveIngredientsByUserIdAsync(IQueryCollection query, int currentUserId);
        Task<(int StatusCode, object Body)> GetAllActiveInactiveIngredientsAsync(IQueryCollection query, int currentUserId);
        Task<(int StatusCode, object Body)> GetAllIngredientsByVendorIdAsync(IQueryCollection query, int vendorId, int currentUserId);
        Task<(int StatusCode, object Body)> GetAllIngredientsByUserIdParamAsync(int userId, IQueryCollection query);
        Task<(int StatusCode, object Body)> UpdateIngredientAsync(Ingredient model, int currentUserId);
        Task<(int StatusCode, object Body)> PatchIngredientDetailsAsync(UpdateIngredientDetailsDto model, int currentUserId, string requestId);
        Task<(int StatusCode, object Body)> ToggleIngredientActiveAsync(int id, int currentUserId);
    }
}
