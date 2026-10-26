using MenuOptics.Models;
using MenuOptics.Models.DTOs;

namespace MenuOptics.Services
{
    public interface IPrivateVendorService
    {
        Task<ResponseDto> GetAllPrivateVendorsAsync(int restaurantId, bool? isActive);
        Task<ResponseDto> GetPrivateVendorAsync(int id, int restaurantId);
        Task<ResponseDto> CreatePrivateVendorAsync(CreatePrivateVendorDTO model, int currentUserId, int actualUserId);
        Task<ResponseDto> UpdatePrivateVendorAsync(UpdatePrivateVendorDTO model, int currentUserId, int ActualUserId);
        Task<ResponseDto> TogglePrivateVendorActiveAsync(int id, int currentUserId, int actualUserId);
    }
}
