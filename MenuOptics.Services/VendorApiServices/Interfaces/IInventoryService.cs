using MenuOptics.Models;

namespace MenuOptics.Services.VendorApi
{
    public interface IInventoryService
    {
        Task<ResponseDto> InvoiceInventoryAsync(InvoiceInventoryRequestDto model, int currentUserId, string requestId);
    }
}
