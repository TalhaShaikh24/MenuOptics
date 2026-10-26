using MenuOptics.Models;
using MenuOptics.Models.DTOs;

namespace MenuOptics.Services
{
    public interface IInventoryService
    {
        Task<ResponseDto> GetInventoryInvoicesAsync(int currentUserId);
        Task<ResponseDto> CreateInvoiceAsync(CreateInvoiceInventoryDTO model, int currentUserId);
        Task<ResponseDto> UpdateInvoiceAsync(UpdateInvoiceInventoryDTO model, int currentUserId);
    }
}
