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
        Task<ResponseDto> AddRestaurantMemberAsync(AddRestaurantMemberDto model, int currentUserId, string callerRole, string isParentClaim);
        Task<ResponseDto> GetRestaurantMembersAsync(int currentUserId);
        Task<ResponseDto> GetMemberStatsAsync(int currentUserId);
        Task<ResponseDto> UpdateRestaurantMemberAsync(UpdateRestaurantMemberDto model, int currentUserId, string requestBaseUrl);
        Task<ResponseDto> ToggleMemberActiveAsync(int id, int currentUserId);
        Task<ResponseDto> CompleteRegistrationAsync(CompleteRegistrationDto model, string requestBaseUrl);
    }
}
