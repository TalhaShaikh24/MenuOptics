using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Models;
using MenuOptics.Services;
using System.Security.Claims;
using MenuOptics.API.Middleware;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [SubscriptionRequired]
    [Authorize(Roles = "Restaurant")]
    public class PrivateVendorApiController : ControllerBase
    {
        private readonly IPrivateVendorService _privateVendorService;

        public PrivateVendorApiController(IPrivateVendorService privateVendorService)
        {
            _privateVendorService = privateVendorService;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpGet("GetAllPrivateVendors")]
        public async Task<IActionResult> GetAllPrivateVendors()
        {
            var responseData = await _privateVendorService.GetAllPrivateVendorsAsync(CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpGet("GetPrivateVendor/{id}")]
        public async Task<IActionResult> GetPrivateVendor(int id)
        {
            var responseData = await _privateVendorService.GetPrivateVendorAsync(id, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPost("CreatePrivateVendor")]
        public async Task<IActionResult> CreatePrivateVendor([FromBody] PrivateVendor model)
        {
            var responseData = await _privateVendorService.CreatePrivateVendorAsync(model, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPut("UpdatePrivateVendor")]
        public async Task<IActionResult> UpdatePrivateVendor([FromBody] PrivateVendor model)
        {
            var responseData = await _privateVendorService.UpdatePrivateVendorAsync(model, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPut("TogglePrivateVendorActive/{id}")]
        public async Task<IActionResult> TogglePrivateVendorActive(int id)
        {
            var responseData = await _privateVendorService.TogglePrivateVendorActiveAsync(id, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }
    }
}
