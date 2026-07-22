using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IRestaurantService
    {
        Task<Response> GetMyRestaurantsAsync(string? nameIdentifierClaim, string requestId);
        Task<Response> GetMyRestaurantAsync(int id, string? nameIdentifierClaim, string requestId);
        Task<Response> SignupRestaurantAsync(CreateRestaurant model, string? nameIdentifierClaim, string requestId);
        Task<Response> UpdateRestaurantAsync(UpdateRestaurant model, string? nameIdentifierClaim, string requestId);
        Task<Response> ToggleRestaurantActiveAsync(int restaurantId, RestaurantStatusRequest request, string? nameIdentifierClaim, string requestId);
        Task<Response> SetPasswordAsync(SetPasswordDTO model, string? nameIdentifierClaim);
    }
}
