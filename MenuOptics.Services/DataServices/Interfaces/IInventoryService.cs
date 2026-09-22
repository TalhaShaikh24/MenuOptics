using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IInventoryService
    {
        Task<ResponseDto> UpdateInventorySuiteReportAsync(InventorySuiteUpdateDto model, int currentUserId);
        Task<ResponseDto> GetInventoryInvoicesAsync(int currentUserId);
        Task<ResponseDto> CreateInvoiceAsync(CreateInvoiceRequestDto model, int currentUserId);
        Task<ResponseDto> UpdateInvoiceAsync(UpdateInvoiceRequestDto model, int currentUserId);
    }
}
