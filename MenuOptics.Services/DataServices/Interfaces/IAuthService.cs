using MenuOptics.Models;
using System.Threading.Tasks;

namespace MenuOptics.Services
{
    public interface IAuthService
    {
        Task<ResponseDto> LoginAsync(LoginDto model, string? ipAddress);
        Task<ResponseDto> LogoutAsync();
        Task<ResponseDto> RefreshTokenAsync(RefreshTokenRequestDto model);
        Task<ResponseDto> RevokeTokenAsync(RefreshTokenRequestDto model);
        Task<ResponseDto> CheckUserAsync(CheckUserDto model);
        Task<ResponseDto> SendOtpAsync(SendOtpDto model);
        Task<ResponseDto> VerifyOtpAsync(VerifyOtpDto model);
    }
}
