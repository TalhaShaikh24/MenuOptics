using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface ISalesService
    {
        Task<ResponseDto> CreateSaleAsync(Sale model, int currentUserId);
        Task<ResponseDto> GetAllSalesAsync();
        Task<ResponseDto> ImportSalesAsync(IFormFile file, int currentUserId);
    }
}
