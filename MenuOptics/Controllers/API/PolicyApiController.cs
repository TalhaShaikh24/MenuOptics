using Microsoft.AspNetCore.Mvc;
using MenuOptics.Models;
using MenuOptics.Services;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class PolicyApiController : ControllerBase
    {
        private readonly IPolicyService _policyService;

        public PolicyApiController(IPolicyService policyService)
        {
            _policyService = policyService;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpGet("GetAllPolicies")]
        public async Task<IActionResult> GetAllPolicies()
        {
            var response = await _policyService.GetAllPoliciesAsync();
            return StatusCode(response.Status, response);
        }

        [HttpGet("GetPolicyByType")]
        public async Task<IActionResult> GetPolicyByType(string type)
        {
            var response = await _policyService.GetPolicyByTypeAsync(type);
            return StatusCode(response.Status, response);
        }

        [HttpPost("SavePolicy")]
        public async Task<IActionResult> SavePolicy([FromBody] PolicyDTO model)
        {
            var response = await _policyService.SavePolicyAsync(model, CurrentUserId);
            return StatusCode(response.Status, response);
        }
    }
}
