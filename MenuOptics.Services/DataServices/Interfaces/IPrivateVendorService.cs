using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IPrivateVendorService
    {
        Task<ResponseDto> GetAllPrivateVendorsAsync(int restaurantId, bool? isActive);
        Task<ResponseDto> GetPrivateVendorAsync(int id, int restaurantId);
        Task<ResponseDto> CreatePrivateVendorAsync(PrivateVendor model, int currentUserId, int actualUserId);
        Task<ResponseDto> UpdatePrivateVendorAsync(PrivateVendor model, int currentUserId, int ActualUserId);
        Task<ResponseDto> TogglePrivateVendorActiveAsync(int id, int currentUserId, int actualUserId);
    }
}
