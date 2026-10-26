using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Services;
using System.Security.Claims;
using MenuOptics.API.Middleware;
using MenuOptics.Models.DTOs;

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
        private int ActualUserId => int.Parse(User.FindFirst("ActualUserId")?.Value ?? "0");

        [HttpGet("GetAllPrivateVendors/{isActive?}")]
        public async Task<IActionResult> GetAllPrivateVendors(bool? isActive)
        {
            var responseData = await _privateVendorService.GetAllPrivateVendorsAsync(CurrentUserId, isActive);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpGet("GetPrivateVendor/{id}")]
        public async Task<IActionResult> GetPrivateVendor(int id)
        {
            var responseData = await _privateVendorService.GetPrivateVendorAsync(id, CurrentUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPost("CreatePrivateVendor")]
        public async Task<IActionResult> CreatePrivateVendor([FromBody] CreatePrivateVendorDTO model)
        {
            var responseData = await _privateVendorService.CreatePrivateVendorAsync(model, CurrentUserId, ActualUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPut("UpdatePrivateVendor")]
        public async Task<IActionResult> UpdatePrivateVendor([FromBody] UpdatePrivateVendorDTO model)
        {
            var responseData = await _privateVendorService.UpdatePrivateVendorAsync(model, CurrentUserId, ActualUserId);
            return StatusCode(responseData.Status, responseData);
        }

        [HttpPut("TogglePrivateVendorActive/{id}")]
        public async Task<IActionResult> TogglePrivateVendorActive(int id)
        {
            var responseData = await _privateVendorService.TogglePrivateVendorActiveAsync(id, CurrentUserId, ActualUserId);
            return StatusCode(responseData.Status, responseData);
        }
    }
}
