using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface ISalesService
    {
        Task<ResponseDto> CreateSaleAsync(Sale model, int currentUserId);
        Task<ResponseDto> GetAllSalesAsync(int CurrentUserId);
        Task<ResponseDto> ImportSalesAsync(IFormFile file, DateTime startDate, DateTime endDate, int currentUserId);
    }
}
