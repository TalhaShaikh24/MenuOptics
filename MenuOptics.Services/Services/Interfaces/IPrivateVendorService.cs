using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IPrivateVendorService
    {
        Task<ResponseDto> GetAllPrivateVendorsAsync(int restaurantId);
        Task<ResponseDto> GetPrivateVendorAsync(int id, int restaurantId);
        Task<ResponseDto> CreatePrivateVendorAsync(PrivateVendor model, int currentUserId);
        Task<ResponseDto> UpdatePrivateVendorAsync(PrivateVendor model, int currentUserId);
        Task<ResponseDto> TogglePrivateVendorActiveAsync(int id, int currentUserId);
    }
}
