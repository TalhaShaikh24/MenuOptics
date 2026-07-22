using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IVendorService
    {
        Task<Response> RegisterVendorAsync(User model, string requestBaseUrl);
    }
}
