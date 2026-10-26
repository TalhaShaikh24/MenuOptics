using MenuOptics.Models;
using MenuOptics.Models.DTOs;

namespace MenuOptics.Services.VendorApi
{
    public interface IInventoryService
    {
        Task<ResponseDto> InvoiceInventoryAsync(CreateVendorInvoiceInventoryDTO model, int currentUserId);
    }
}
