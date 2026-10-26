using MenuOptics.Models;
using MenuOptics.Models.DTOs.Category;

namespace MenuOptics.Services
{
    public interface ICategoryService
    {
        Task<ResponseDto> GetAllCategoriesAsync(int restaurantId, bool? isActive);
        Task<ResponseDto> GetCategoryAsync(int id, int restaurantId);
        Task<ResponseDto> CreateCategoryAsync(CreateCategoryDTO model, int currentUserId, int actualUserId);
        Task<ResponseDto> UpdateCategoryAsync(UpdateCategoryDTO model, int currentUserId, int actualUserId);
        Task<ResponseDto> ToggleCategoryActiveAsync(int id, int currentUserId, int actualUserId);
    }
}
