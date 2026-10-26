using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IPolicyService
    {
        Task<ResponseDto> GetAllPoliciesAsync();
        Task<ResponseDto> GetPolicyByTypeAsync(string type);
        Task<ResponseDto> SavePolicyAsync(SavePolicyDTO model, int currentUserId);
    }
}
