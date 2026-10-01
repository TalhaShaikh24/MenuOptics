using MenuOptics.Controllers.Api;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace MenuOptics.Tests.Controllers
{
    public class EventApiControllerTests
    {
        private readonly Mock<IEventService> _mockService;
        private readonly EventApiController _controller;

        public EventApiControllerTests()
        {
            _mockService = new Mock<IEventService>();
            _controller = new EventApiController(_mockService.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim("ActualUserId", "1"),
                new Claim(ClaimTypes.Role, "Restaurant")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        /* ── GetAllEvents ─────────────────────────────────────── */

        [Fact]
        public async Task GetAllEvents_Returns200_WhenEventsExist()
        {
            _mockService.Setup(s => s.GetAllEventsAsync(1))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.GetAllEvents() as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        /* ── GetEvent ─────────────────────────────────────────── */

        [Fact]
        public async Task GetEvent_Returns200_WhenFound()
        {
            _mockService.Setup(s => s.GetEventAsync(5, 1))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.GetEvent(5) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetEvent_Returns404_WhenNotFound()
        {
            _mockService.Setup(s => s.GetEventAsync(999, 1))
                        .ReturnsAsync(new ResponseDto { Status = 404 });

            var result = await _controller.GetEvent(999) as ObjectResult;

            Assert.Equal(404, result!.StatusCode);
        }

        /* ── CreateEvent ──────────────────────────────────────── */

        [Fact]
        public async Task CreateEvent_Returns201_WhenCreated()
        {
            var model = new Event { EventName = "Gala Dinner" };
            _mockService.Setup(s => s.CreateEventAsync(model, 1, 1))
                        .ReturnsAsync(new ResponseDto { Status = 201 });

            var result = await _controller.CreateEvent(model) as ObjectResult;

            Assert.Equal(201, result!.StatusCode);
        }

        /* ── UpdateEvent ──────────────────────────────────────── */

        [Fact]
        public async Task UpdateEvent_Returns200_WhenUpdated()
        {
            var model = new Event { EventId = 1, EventName = "Updated Gala" };
            _mockService.Setup(s => s.UpdateEventAsync(model, 1, 1))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.UpdateEvent(model) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }

        /* ── DuplicateEvent ───────────────────────────────────── */

        [Fact]
        public async Task DuplicateEvent_Returns201_WhenDuplicated()
        {
            _mockService.Setup(s => s.DuplicateEventAsync(2))
                        .ReturnsAsync(new ResponseDto { Status = 201 });

            var result = await _controller.DuplicateEvent(2) as ObjectResult;

            Assert.Equal(201, result!.StatusCode);
        }

        /* ── ToggleEventActive ────────────────────────────────── */

        [Fact]
        public async Task ToggleEventActive_Returns200_WhenToggled()
        {
            _mockService.Setup(s => s.ToggleEventActiveAsync(1, 1, 1))
                        .ReturnsAsync(new ResponseDto { Status = 200 });

            var result = await _controller.ToggleEventActive(1) as ObjectResult;

            Assert.Equal(200, result!.StatusCode);
        }
    }
}
