using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IInventoryService
    {
        Task<ResponseDto> UpdateClosingCountAsync(ClosingCountUpdateDto model, int currentUserId);
        Task<ResponseDto> GetInventoryInvoicesAsync(int currentUserId);
    }
}
