using MenuOptics.Controllers.Api;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace MenuOptics.Tests.Controllers
{
    public class SubscriptionApiControllerTests
    {
        private readonly Mock<ISubscriptionService> _mockService;

        private SubscriptionApiController CreateController(bool isVendorCreated = false, bool isParent = true)
        {
            var mockSvc = _mockService;
            var ctrl = new SubscriptionApiController(mockSvc.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim("ActualUserId", "1"),
                new Claim("IsParent", isParent.ToString()),
                new Claim("IsVendorCreated", isVendorCreated.ToString()),
                new Claim(ClaimTypes.Role, "Restaurant")
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
            return ctrl;
        }

        public SubscriptionApiControllerTests()
        {
            _mockService = new Mock<ISubscriptionService>();
        }

        /* ── GetDetails ───────────────────────────────────────── */

        [Fact]
        public async Task GetDetails_Returns200_ForVendorCreatedRestaurant()
        {
            var ctrl = CreateController(isVendorCreated: true);
            var result = await ctrl.GetDetails() as OkObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetDetails_Returns200_ForRegularRestaurant()
        {
            _mockService.Setup(s => s.GetSubscriptionDetailsAsync(1, 1, true))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var ctrl = CreateController(isVendorCreated: false);
            var result = await ctrl.GetDetails() as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        /* ── Cancel ───────────────────────────────────────────── */

        [Fact]
        public async Task Cancel_Returns400_ForVendorCreatedRestaurant()
        {
            var ctrl = CreateController(isVendorCreated: true);
            var result = await ctrl.Cancel() as ObjectResult;

            Assert.Equal(400, result!.StatusCode);
        }

        [Fact]
        public async Task Cancel_Returns200_ForRegularRestaurant()
        {
            _mockService.Setup(s => s.CancelSubscriptionAsync(1, 1, true))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var ctrl = CreateController(isVendorCreated: false);
            var result = await ctrl.Cancel() as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        /* ── Renew ────────────────────────────────────────────── */

        [Fact]
        public async Task Renew_Returns400_ForVendorCreatedRestaurant()
        {
            var ctrl = CreateController(isVendorCreated: true);
            var result = await ctrl.Renew(new MenuOptics.Models.DTOs.Subscription.RenewSubscriptionRequestDto()) as ObjectResult;

            Assert.Equal(400, result!.StatusCode);
        }

        [Fact]
        public async Task Renew_Returns403_WhenNotParent()
        {
            var ctrl = CreateController(isVendorCreated: false, isParent: false);
            var result = await ctrl.Renew(new MenuOptics.Models.DTOs.Subscription.RenewSubscriptionRequestDto()) as ObjectResult;

            Assert.Equal(403, result!.StatusCode);
        }
    }
}
