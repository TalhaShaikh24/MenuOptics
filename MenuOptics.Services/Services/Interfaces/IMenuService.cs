using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IMenuService
    {
        Task<ResponseDto> CreateMenuItemAsync(Menu model, string ingredients, string recipes, int currentUserId);
        Task<ResponseDto> GetMenuItemAsync(int id);
        Task<ResponseDto> GetAllMenuItemsByAdminAsync();
        Task<ResponseDto> GetAllMenuItemsAsync(int currentUserId);
        Task<ResponseDto> UpdateMenuItemAsync(Menu model, string ingredients, string recipes, int currentUserId);
        Task<ResponseDto> ToggleMenuActiveAsync(int id, int currentUserId);
    }
}
