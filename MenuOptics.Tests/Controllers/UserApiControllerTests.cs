using MenuOptics.Controllers.Api;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace MenuOptics.Tests.Controllers
{
    public class UserApiControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly UserApiController _controller;

        public UserApiControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockAuthService = new Mock<IAuthService>();
            _controller = new UserApiController(_mockUserService.Object, _mockAuthService.Object);

            // Setup fake authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "10"),
                new Claim(ClaimTypes.Role, "Restaurant")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        /* ── Login ────────────────────────────────────────────── */

        [Fact]
        public async Task Login_Returns200_WhenCredentialsAreValid()
        {
            // Arrange
            var loginDto = new LoginDto { Username = "test@test.com", Password = "Pass@123" };
            var expected = new ResponseDto { Status = 200, ResponseMsg = "Login successful" };

            _mockAuthService
                .Setup(s => s.LoginAsync(loginDto, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(expected);

            // Act
            var result = await _controller.Login(loginDto) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task Login_Returns401_WhenCredentialsAreInvalid()
        {
            // Arrange
            var loginDto = new LoginDto { Username = "wrong@test.com", Password = "wrong" };
            var expected = new ResponseDto { Status = 401, ResponseMsg = "Invalid credentials" };

            _mockAuthService
                .Setup(s => s.LoginAsync(loginDto, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(expected);

            // Act
            var result = await _controller.Login(loginDto) as ObjectResult;

            // Assert
            Assert.Equal(401, result!.StatusCode);
        }

        [Fact]
        public async Task Login_CallsAuthService_WithRequestIdAndIp()
        {
            // Arrange
            var loginDto = new LoginDto { Username = "a@b.com", Password = "Pass@1" };
            var expected = new ResponseDto { Status = 200 };

            _mockAuthService
                .Setup(s => s.LoginAsync(loginDto, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(expected);

            // Act
            await _controller.Login(loginDto);

            // Assert — verify the service was called with some requestId (not null/empty)
            _mockAuthService.Verify(s => s.LoginAsync(
                loginDto,
                It.Is<string>(id => !string.IsNullOrEmpty(id)),
                It.IsAny<string?>()
            ), Times.Once);
        }

        /* ── GetAllVendors (AllowAnonymous) ───────────────────── */

        [Fact]
        public async Task GetAllVendors_Returns200_WhenVendorsExist()
        {
            // Arrange
            var expected = new ResponseDto { Status = 200 };
            _mockUserService.Setup(s => s.GetAllVendorsAsync(It.IsAny<int>())).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetAllVendors() as ObjectResult;

            // Assert
            Assert.Equal(200, result!.StatusCode);
        }

        /* ── GetUser ──────────────────────────────────────────── */

        [Fact]
        public async Task GetUser_Returns200_WhenUserExists()
        {
            // Arrange
            var expected = new ResponseDto { Status = 200 };
            _mockUserService.Setup(s => s.GetUserAsync(5)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetUser(5) as ObjectResult;

            // Assert
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetUser_Returns404_WhenUserNotFound()
        {
            // Arrange
            var expected = new ResponseDto { Status = 404, ResponseMsg = "User not found" };
            _mockUserService.Setup(s => s.GetUserAsync(999)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetUser(999) as ObjectResult;

            // Assert
            Assert.Equal(404, result!.StatusCode);
        }

        /* ── Logout ───────────────────────────────────────────── */

        [Fact]
        public async Task Logout_Returns200_Always()
        {
            // Arrange
            var expected = new ResponseDto { Status = 200, ResponseMsg = "Logged out" };
            _mockAuthService.Setup(s => s.LogoutAsync()).ReturnsAsync(expected);

            // Act
            var result = await _controller.Logout() as ObjectResult;

            // Assert
            Assert.Equal(200, result!.StatusCode);
        }

        /* ── RefreshToken ─────────────────────────────────────── */

        [Fact]
        public async Task RefreshToken_Returns200_WhenTokenIsValid()
        {
            // Arrange
            var model = new RefreshTokenRequestDto { RefreshToken = "valid-token" };
            var expected = new ResponseDto { Status = 200 };
            _mockAuthService.Setup(s => s.RefreshTokenAsync(model)).ReturnsAsync(expected);

            // Act
            var result = await _controller.RefreshToken(model) as ObjectResult;

            // Assert
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task RefreshToken_Returns401_WhenTokenIsExpired()
        {
            // Arrange
            var model = new RefreshTokenRequestDto { RefreshToken = "expired-token" };
            var expected = new ResponseDto { Status = 401, ResponseMsg = "Token expired" };
            _mockAuthService.Setup(s => s.RefreshTokenAsync(model)).ReturnsAsync(expected);

            // Act
            var result = await _controller.RefreshToken(model) as ObjectResult;

            // Assert
            Assert.Equal(401, result!.StatusCode);
        }
    }
}
