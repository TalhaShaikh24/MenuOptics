using MenuOptics.Models;

namespace MenuOptics.Services.VendorApi
{
    public interface IRestaurantService
    {
        Task<ResponseDto> SignupRestaurantAsync(CreateRestaurantDTO model, int currentUserId);
        Task<ResponseDto> UpdateRestaurantAsync(UpdateRestaurantDTO model, int currentUserId);
        Task<ResponseDto> ToggleRestaurantActiveAsync(int restaurantId, RestaurantStatusRequestDTO request, int currentUserId);
    }
}
