using MenuOptics.Models;

namespace MenuOptics.Services.VendorApi
{
    public interface IIngredientService
    {
        Task<(int StatusCode, object Body)> CreateIngredientByVendorAsync(VendorIngredientDto model, int currentUserId);
        Task<(int StatusCode, object Body)> UpdateIngredientPricesAsync(UpdateIngredientPriceDto model, int currentUserId);
        Task<(int StatusCode, object Body)> PatchIngredientDetailsAsync(UpdateIngredientDetailsDto model, int currentUserId);
    }
}
