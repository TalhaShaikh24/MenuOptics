using MenuOptics.Models;
using MenuOptics.Models.DTOs;
using System.Threading.Tasks;

namespace MenuOptics.Services
{
    public interface IAuthService
    {
        Task<ResponseDto> LoginAsync(LoginDTO model, string? ipAddress);
        Task<ResponseDto> LogoutAsync();
        Task<ResponseDto> RefreshTokenAsync(RefreshTokenRequestDTO model);
        Task<ResponseDto> RevokeTokenAsync(RefreshTokenRequestDTO model);
        Task<ResponseDto> CheckUserAsync(CheckUserDTO model);
        Task<ResponseDto> SendOtpAsync(SendOtpDTO model);
        Task<ResponseDto> VerifyOtpAsync(VerifyOtpDTO model);
    }
}
