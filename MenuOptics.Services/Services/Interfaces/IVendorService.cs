using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IVendorService
    {
        Task<ResponseDto> RegisterVendorAsync(User model, string requestBaseUrl);
    }
}
