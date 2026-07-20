using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IRecipeService
    {
        Task<Response> CreateRecipeAsync(Recipe model, string ingredients, int currentUserId);
        Task<Response> UpdateRecipeAsync(Recipe model, string ingredients, int currentUserId);
        Task<Response> GetRecipeAsync(int id);
        Task<Response> GetAllRecipesAsync(int currentUserId);
        Task<Response> DeleteRecipeAsync(int id, int currentUserId);
        Task<Response> ToggleRecipeActiveAsync(int id, int currentUserId);
    }
}
