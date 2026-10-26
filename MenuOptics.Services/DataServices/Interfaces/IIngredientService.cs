using MenuOptics.Models;
using MenuOptics.Models.DTOs.Ingredient;
using Microsoft.AspNetCore.Http;

namespace MenuOptics.Services
{
    public interface IIngredientService
    {
        Task<(int StatusCode, object Body)> CreateIngredientAsync(CreateIngredientDTO model, int currentUserId, int actualUserId);
        Task<(int StatusCode, object Body)> UpdateIngredientAsync(UpdateIngredientDTO model, int currentUserId, int actualUserId);
        Task<(int StatusCode, object Body)> GetIngredientAsync(int id);
        Task<(int StatusCode, object Body)> GetAllActiveIngredientsByUserIdAsync(IQueryCollection query, int currentUserId);
        Task<(int StatusCode, object Body)> GetAllActiveInactiveIngredientsAsync(IQueryCollection query, int currentUserId);
        Task<(int StatusCode, object Body)> GetAllIngredientsByVendorIdAsync(IQueryCollection query, int vendorId, int currentUserId, bool isPrivate);
        Task<(int StatusCode, object Body)> GetAllIngredientsByUserIdParamAsync(int userId, IQueryCollection query);
        Task<(int StatusCode, object Body)> ToggleIngredientActiveAsync(int id, int currentUserId, int actualUserId);
    }
}
