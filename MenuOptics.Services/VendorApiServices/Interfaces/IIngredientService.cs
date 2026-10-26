using MenuOptics.Models;

namespace MenuOptics.Services.VendorApi
{
    public interface IIngredientService
    {
        Task<(int StatusCode, object Body)> CreateIngredientByVendorAsync(CreateVendorIngredientDTO model, int currentUserId);
        Task<(int StatusCode, object Body)> UpdateIngredientPricesAsync(UpdateVendorIngredientPriceDTO model, int currentUserId);
        Task<(int StatusCode, object Body)> PatchIngredientDetailsAsync(UpdateVendorIngredientDetailsDTO model, int currentUserId);
    }
}
