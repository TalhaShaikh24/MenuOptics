using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IUserService
    {
        Task<Response> GetUserStatsAsync();
        Task<Response> GetRestaurantStatsAsync();
        Task<Response> CreateUserAsync(User model, int fallbackCreatedBy, string requestBaseUrl);
        Task<Response> GetUserAsync(int id);
        Task<Response> GetAllUsersAsync();
        Task<Response> GetAllRestaurantsAsync();
        Task<Response> UpdateUserAsync(User model, int modifiedBy, string requestBaseUrl);
        Task<Response> ToggleUserActiveAsync(int id, int currentUserId);
        Task<Response> DeleteUserAsync(int id);
        Task<Response> LoginAsync(LoginModel model, string requestId);
        Task<Response> LogoutAsync();
        Task<Response> AddRestaurantMemberAsync(AddRestaurantMemberDTO model, int currentUserId, string callerRole, string isParentClaim);
        Task<Response> GetRestaurantMembersAsync(int currentUserId);
        Task<Response> GetMemberStatsAsync(int currentUserId);
        Task<Response> UpdateRestaurantMemberAsync(UpdateRestaurantMemberDTO model, int currentUserId, string requestBaseUrl);
        Task<Response> ToggleMemberActiveAsync(int id, int currentUserId);
        Task<Response> DeleteMemberAsync(int id, int currentUserId);
        string CreateCheckoutSession(string? plan, int userId);
        Task<Response> RefreshTokenAsync(RefreshTokenRequest model);
        Task<Response> RevokeTokenAsync(RefreshTokenRequest model);
        Task<Response> CheckEmailAsync(CheckEmailModel model);
        Task<Response> CompleteRegistrationAsync(CompleteRegistrationDTO model, string requestBaseUrl);
        Task<Response> SendOtpAsync(SendOtpDTO model);
        Task<Response> VerifyOtpAsync(VerifyOtpDTO model);
    }
}
