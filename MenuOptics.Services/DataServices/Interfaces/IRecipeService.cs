using MenuOptics.Models;
using MenuOptics.Models.DTOs.Recipe;

namespace MenuOptics.Services
{
    public interface IRecipeService
    {
        Task<ResponseDto> CreateRecipeAsync(CreateRecipeDTO model, string ingredients, int currentUserId, int actualUserId);
        Task<ResponseDto> UpdateRecipeAsync(UpdateRecipeDTO model, string ingredients, int currentUserId, int actualUserId);
        Task<ResponseDto> GetRecipeAsync(int id);
        Task<ResponseDto> GetAllRecipesAsync(int currentUserId);
        Task<ResponseDto> ToggleRecipeActiveAsync(int id, int currentUserId, int actualUserId);
    }
}
