using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IIngredientService
    {
        Task<(int StatusCode, object Body)> CreateIngredientByVendorAsync(VendorIngredientRequest model, int currentUserId, string requestId);
        Task<(int StatusCode, object Body)> UpdateIngredientPricesAsync(UpdateIngredientPriceRequest model, int currentUserId, string requestId);
        Task<(int StatusCode, object Body)> CreateIngredientAsync(Ingredient model, int currentUserId);
        Task<(int StatusCode, object Body)> AddIngredientStockAsync(Inventory model, int currentUserId);
        Task<(int StatusCode, object Body)> GetIngredientAsync(int id);
        Task<(int StatusCode, object Body)> GetAllIngredientsAsync(IQueryCollection query);
        Task<(int StatusCode, object Body)> GetAllIngredientsByUserIdAsync(IQueryCollection query, int currentUserId);
        Task<(int StatusCode, object Body)> GetAllActiveInactiveIngredientsAsync(IQueryCollection query, int currentUserId);
        Task<(int StatusCode, object Body)> GetAllIngredientsByVendorIdAsync(IQueryCollection query, int vendorId);
        Task<(int StatusCode, object Body)> GetAllIngredientsByUserIdParamAsync(int userId, IQueryCollection query);
        Task<(int StatusCode, object Body)> UpdateIngredientAsync(Ingredient model, int currentUserId);
        Task<(int StatusCode, object Body)> PatchIngredientDetailsAsync(UpdateIngredientDetailsRequest model, int currentUserId, string requestId);
        Task<(int StatusCode, object Body)> DeleteIngredientByIdAsync(int id, int currentUserId);
        Task<(int StatusCode, object Body)> ToggleIngredientActiveAsync(int id, int currentUserId);
        Task<(int StatusCode, object Body)> GetAllVendorsAsync(int currentUserId);
        Task<(int StatusCode, object Body)> LinkVendorAsync(LinkVendorRequest model, int currentUserId);
        Task<(int StatusCode, object Body)> UnlinkVendorAsync(int vendorId, int currentUserId);
    }
}
