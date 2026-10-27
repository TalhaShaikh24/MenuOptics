using MenuOptics.Models;
using MenuOptics.Models.DTOs;

namespace MenuOptics.Services
{
    public interface IMenuService
    {
        Task<ResponseDto> CreateMenuItemAsync(CreateMenuItemDTO model, string ingredients, string recipes, int currentUserId, int actualUserId);
        Task<ResponseDto> UpdateMenuItemAsync(UpdateMenuItemDTO model, string ingredients, string recipes, int currentUserId, int actualUserId);
        Task<ResponseDto> GetMenuItemAsync(int id);
        Task<ResponseDto> GetAllMenuItemsAsync(bool? isActive, int currentUserId);
        Task<ResponseDto> ToggleMenuActiveAsync(int id, int currentUserId, int actualUserId);
    }
}
