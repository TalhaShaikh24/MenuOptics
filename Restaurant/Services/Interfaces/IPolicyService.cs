using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IPolicyService
    {
        Task<Response> GetAllPoliciesAsync();
        Task<Response> GetPolicyByTypeAsync(string type);
        Task<Response> SavePolicyAsync(PolicyDTO model, int currentUserId);
    }
}
