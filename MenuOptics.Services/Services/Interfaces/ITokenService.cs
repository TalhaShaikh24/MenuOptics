using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(UserDto user);
        string GenerateRefreshToken();
        Task SaveRefreshTokenAsync(int userId, string refreshToken);
        Task<RefreshToken?> GetRefreshTokenAsync(string token);
        Task RevokeRefreshTokenAsync(string token);
    }
}
