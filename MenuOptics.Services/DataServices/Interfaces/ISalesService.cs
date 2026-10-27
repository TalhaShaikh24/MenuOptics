using MenuOptics.Models;
using MenuOptics.Models.DTOs;

namespace MenuOptics.Services
{
    public interface ISalesService
    {
        Task<ResponseDto> CreateSaleAsync(CreateSaleDTO model, int currentUserId);
        Task<ResponseDto> UpdateSaleAsync(UpdateSaleDTO model, int currentUserId);
        Task<ResponseDto> GetAllSalesAsync(int CurrentUserId);
        Task<ResponseDto> ImportSalesAsync(IFormFile file, DateTime startDate, DateTime endDate, int currentUserId);
    }
}
