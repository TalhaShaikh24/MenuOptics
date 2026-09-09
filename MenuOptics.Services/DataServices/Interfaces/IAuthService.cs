using MenuOptics.Models;
using System.Threading.Tasks;

namespace MenuOptics.Services
{
    public interface IAuthService
    {
        Task<ResponseDto> LoginAsync(LoginDto model, string requestId);
        Task<ResponseDto> LogoutAsync();
        (string SessionId, string? Url) CreateCheckoutSession(string? plan, int userId);
        Task<ResponseDto> RefreshTokenAsync(RefreshTokenRequestDto model);
        Task<ResponseDto> RevokeTokenAsync(RefreshTokenRequestDto model);
        Task<ResponseDto> CheckUserAsync(CheckUserDto model);
        Task<ResponseDto> SendOtpAsync(SendOtpDto model);
        Task<ResponseDto> VerifyOtpAsync(VerifyOtpDto model);
        Task SignInWithCookieAsync(UserDto user, int effectiveUserId, bool isParent);
    }
}
