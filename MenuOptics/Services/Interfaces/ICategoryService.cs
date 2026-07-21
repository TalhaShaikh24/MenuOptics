using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface ICategoryService
    {
        Task<Response> GetAllCategoriesAsync(int restaurantId);
        Task<Response> GetCategoryAsync(int id, int restaurantId);
        Task<Response> CreateCategoryAsync(Category model, int currentUserId);
        Task<Response> UpdateCategoryAsync(Category model, int currentUserId);
        Task<Response> ToggleCategoryActiveAsync(int id, int currentUserId);
        Task<Response> DeleteCategoryByIdAsync(int id, int currentUserId);
    }
}
