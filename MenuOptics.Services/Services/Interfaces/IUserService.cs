using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IUserService
    {
        Task<ResponseDto> GetUserStatsAsync();
        Task<ResponseDto> GetRestaurantStatsAsync();
        Task<ResponseDto> CreateUserAsync(User model, int fallbackCreatedBy, string requestBaseUrl);
        Task<ResponseDto> GetUserAsync(int id);
        Task<ResponseDto> GetAllUsersAsync();
        Task<ResponseDto> GetAllRestaurantsAsync();
        Task<ResponseDto> GetAllVendorsAsync(int currentUserId);
        Task<ResponseDto> UpdateUserAsync(User model, int modifiedBy, string requestBaseUrl);
        Task<ResponseDto> ToggleUserActiveAsync(int id, int currentUserId);
        Task<ResponseDto> LoginAsync(LoginDto model, string requestId);
        Task<ResponseDto> LogoutAsync();
        Task<ResponseDto> AddRestaurantMemberAsync(AddRestaurantMemberDto model, int currentUserId, string callerRole, string isParentClaim);
        Task<ResponseDto> GetRestaurantMembersAsync(int currentUserId);
        Task<ResponseDto> GetMemberStatsAsync(int currentUserId);
        Task<ResponseDto> UpdateRestaurantMemberAsync(UpdateRestaurantMemberDto model, int currentUserId, string requestBaseUrl);
        Task<ResponseDto> ToggleMemberActiveAsync(int id, int currentUserId);
        string CreateCheckoutSession(string? plan, int userId);
        Task<ResponseDto> RefreshTokenAsync(RefreshTokenRequestDto model);
        Task<ResponseDto> RevokeTokenAsync(RefreshTokenRequestDto model);
        Task<ResponseDto> CheckEmailAsync(CheckEmailDto model);
        Task<ResponseDto> CompleteRegistrationAsync(CompleteRegistrationDto model, string requestBaseUrl);
        Task<ResponseDto> SendOtpAsync(SendOtpDto model);
        Task<ResponseDto> VerifyOtpAsync(VerifyOtpDto model);

    }
}
