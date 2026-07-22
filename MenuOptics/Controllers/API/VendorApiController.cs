using Microsoft.AspNetCore.Mvc;
using MenuOptics.Models;
using MenuOptics.Services;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class VendorApiController : ControllerBase
    {
        private readonly IVendorService _vendorService;

        public VendorApiController(IVendorService vendorService)
        {
            _vendorService = vendorService;
        }

        [HttpPost("RegisterVendor")]
        public async Task<IActionResult> RegisterVendor([FromForm] User model)
        {
            var response = await _vendorService.RegisterVendorAsync(model, $"{Request.Scheme}://{Request.Host}");
            return StatusCode(response.Status, response);
        }
    }
}
