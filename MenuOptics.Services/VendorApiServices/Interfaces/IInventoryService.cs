using MenuOptics.Models;

namespace MenuOptics.Services.VendorApi
{
    public interface IInventoryService
    {
        Task<ResponseDto> InvoiceInventoryAsync(CreateVendorInvoiceInventoryDTO model, int currentUserId);
    }
}
