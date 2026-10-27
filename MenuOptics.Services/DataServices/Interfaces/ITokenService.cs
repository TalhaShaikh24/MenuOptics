using MenuOptics.Models;
using MenuOptics.Models.DTOs;

namespace MenuOptics.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(GetUserDTO user);
        string GenerateRefreshToken();
        Task SaveRefreshTokenAsync(int userId, string refreshToken);
        Task<RefreshTokenDTO?> GetRefreshTokenAsync(string token);
        Task RevokeRefreshTokenAsync(string token);
    }
}
