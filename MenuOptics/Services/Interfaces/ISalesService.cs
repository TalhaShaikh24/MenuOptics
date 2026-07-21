using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface ISalesService
    {
        Task<Response> CreateSaleAsync(Sale model, int currentUserId);
        Task<Response> GetAllSalesAsync();
        Task<Response> ImportSalesAsync(IFormFile file, int currentUserId);
    }
}
