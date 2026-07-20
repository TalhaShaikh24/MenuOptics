using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IMenuService
    {
        Task<Response> CreateMenuItemAsync(Menu model, string ingredients, string recipes, int currentUserId);
        Task<Response> GetMenuItemAsync(int id);
        Task<Response> GetAllMenuItemsByAdminAsync();
        Task<Response> GetAllMenuItemsAsync(int currentUserId);
        Task<Response> UpdateMenuItemAsync(Menu model, string ingredients, string recipes, int currentUserId);
        Task<Response> DeleteMenuItemAsync(int id);
        Task<Response> ToggleMenuActiveAsync(int id, int currentUserId);
    }
}
