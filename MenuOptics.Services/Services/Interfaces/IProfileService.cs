using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IProfileService
    {
        Task<(int StatusCode, object Body)> GetCurrentUserAsync(string? userId);
        Task<(int StatusCode, object Body)> UpdateProfileAsync(User model, int currentUserId, string requestBaseUrl);
    }
}
