using MenuOptics.Controllers.Api;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace MenuOptics.Tests.Controllers
{
    public class PolicyApiControllerTests
    {
        private readonly Mock<IPolicyService> _mockService;
        private readonly PolicyApiController _controller;

        public PolicyApiControllerTests()
        {
            _mockService = new Mock<IPolicyService>();
            _controller = new PolicyApiController(_mockService.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        [Fact]
        public async Task GetAllPolicies_Returns200_WhenPoliciesExist()
        {
            _mockService.Setup(s => s.GetAllPoliciesAsync())
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.GetAllPolicies() as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetPolicyByType_Returns200_WhenFound()
        {
            _mockService.Setup(s => s.GetPolicyByTypeAsync("privacy"))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.GetPolicyByType("privacy") as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetPolicyByType_Returns404_WhenNotFound()
        {
            _mockService.Setup(s => s.GetPolicyByTypeAsync("unknown"))
                        .ReturnsAsync(new ResponseDto { Status = 404 });

            var result = await _controller.GetPolicyByType("unknown") as ObjectResult;

            Assert.Equal(404, result!.StatusCode);
        }

        [Fact]
        public async Task SavePolicy_Returns200_WhenSaved()
        {
            var model = new PolicyDto { PolicyType = "privacy", Content = "Lorem ipsum..." };
            _mockService.Setup(s => s.SavePolicyAsync(model, 1))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.SavePolicy(model) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }
    }
}
