using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface ICategoryService
    {
        Task<ResponseDto> GetAllCategoriesAsync(int restaurantId);
        Task<ResponseDto> GetCategoryAsync(int id, int restaurantId);
        Task<ResponseDto> CreateCategoryAsync(Category model, int currentUserId);
        Task<ResponseDto> UpdateCategoryAsync(Category model, int currentUserId);
        Task<ResponseDto> ToggleCategoryActiveAsync(int id, int currentUserId);
    }
}
