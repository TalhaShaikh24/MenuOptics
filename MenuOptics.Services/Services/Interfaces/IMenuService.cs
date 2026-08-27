using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IMenuService
    {
        Task<ResponseDto> CreateMenuItemAsync(Menu model, string ingredients, string recipes, int currentUserId, int actualUserId);
        Task<ResponseDto> UpdateMenuItemAsync(Menu model, string ingredients, string recipes, int currentUserId, int actualUserId);
        Task<ResponseDto> GetMenuItemAsync(int id);
        Task<ResponseDto> GetAllMenuItemsByAdminAsync();
        Task<ResponseDto> GetAllMenuItemsAsync(bool? isActive, int currentUserId);
        Task<ResponseDto> ToggleMenuActiveAsync(int id, int currentUserId, int actualUserId);
    }
}
