using MenuOptics.Models;

namespace MenuOptics.Services.VendorApi
{
    public interface IRestaurantService
    {
        Task<ResponseDto> SignupRestaurantAsync(CreateRestaurantDto model, string? nameIdentifierClaim, string requestId);
        Task<ResponseDto> UpdateRestaurantAsync(UpdateRestaurantDto model, string? nameIdentifierClaim, string requestId);
        Task<ResponseDto> ToggleRestaurantActiveAsync(int restaurantId, RestaurantStatusRequestDto request, string? nameIdentifierClaim, string requestId);
    }
}
