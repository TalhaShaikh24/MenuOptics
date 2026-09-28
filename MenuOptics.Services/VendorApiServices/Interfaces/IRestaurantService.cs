using MenuOptics.Models;

namespace MenuOptics.Services.VendorApi
{
    public interface IRestaurantService
    {
        Task<ResponseDto> SignupRestaurantAsync(CreateRestaurantDto model, int currentUserId, string requestId);
        Task<ResponseDto> UpdateRestaurantAsync(UpdateRestaurantDto model, int currentUserId, string requestId);
        Task<ResponseDto> ToggleRestaurantActiveAsync(int restaurantId, RestaurantStatusRequestDto request, int currentUserId, string requestId);
    }
}
