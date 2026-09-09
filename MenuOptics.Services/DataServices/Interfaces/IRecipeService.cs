using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IRecipeService
    {
        Task<ResponseDto> CreateRecipeAsync(Recipe model, string ingredients, int currentUserId, int actualUserId);
        Task<ResponseDto> UpdateRecipeAsync(Recipe model, string ingredients, int currentUserId, int actualUserId);
        Task<ResponseDto> GetRecipeAsync(int id);
        Task<ResponseDto> GetAllRecipesAsync(int currentUserId);
        Task<ResponseDto> ToggleRecipeActiveAsync(int id, int currentUserId, int actualUserId);
    }
}
