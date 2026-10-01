using MenuOptics.Controllers.Api;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace MenuOptics.Tests.Controllers
{
    public class InventoryApiControllerTests
    {
        private readonly Mock<IInventoryService> _mockService;
        private readonly InventoryApiController _controller;

        public InventoryApiControllerTests()
        {
            _mockService = new Mock<IInventoryService>();
            _controller = new InventoryApiController(_mockService.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Restaurant")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        /* ── GetInventoryInvoices ─────────────────────────────── */

        [Fact]
        public async Task GetInventoryInvoices_Returns200_WhenInvoicesExist()
        {
            _mockService.Setup(s => s.GetInventoryInvoicesAsync(1))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.GetInventoryInvoices() as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        /* ── CreateInvoice ────────────────────────────────────── */

        [Fact]
        public async Task CreateInvoice_Returns201_WhenCreated()
        {
            var model = new CreateInvoiceRequestDto { PrivateVendorId = 1 };
            _mockService.Setup(s => s.CreateInvoiceAsync(model, 1))
                        .ReturnsAsync(new ResponseDto { Status = 201 });

            var result = await _controller.CreateInvoice(model) as ObjectResult;

            Assert.Equal(201, result!.StatusCode);
        }

        [Fact]
        public async Task CreateInvoice_Returns400_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("VendorId", "Required");
            var model = new CreateInvoiceRequestDto();

            var result = await _controller.CreateInvoice(model) as ObjectResult;

            Assert.Equal(400, result!.StatusCode);
        }

        /* ── UpdateInvoice ────────────────────────────────────── */

        [Fact]
        public async Task UpdateInvoice_Returns200_WhenUpdated()
        {
            var model = new UpdateInvoiceRequestDto { InventoryInvoiceId = 1 };
            _mockService.Setup(s => s.UpdateInvoiceAsync(model, 1))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.UpdateInvoice(model) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        /* ── UpdateInventorySuiteReport ───────────────────────── */

        [Fact]
        public async Task UpdateInventorySuiteReport_Returns200_WhenUpdated()
        {
            var model = new InventorySuiteUpdateDto();
            _mockService.Setup(s => s.UpdateInventorySuiteReportAsync(model, 1))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.UpdateInventorySuiteReport(model) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }
    }
}
