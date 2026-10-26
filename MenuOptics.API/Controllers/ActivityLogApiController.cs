using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Models;
using MenuOptics.Models.DTOs;
using MenuOptics.Services;
using MenuOptics.Services.VendorApiServices.Interfaces;

namespace MenuOptics.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ActivityLogApiController : ControllerBase
    {
        private readonly IActivityLogService _activityLogService;
        private readonly IVendorActivityLogService _vendorActivityLogService;

        public ActivityLogApiController(
            IActivityLogService activityLogService,
            IVendorActivityLogService vendorActivityLogService)
        {
            _activityLogService = activityLogService;
            _vendorActivityLogService = vendorActivityLogService;
        }

        [HttpGet("GetUserActivityLogs")]
        public async Task<IActionResult> GetUserActivityLogs([FromQuery] FilterActivityLogDTO filter)
        {
            try
            {
                var logs = await _activityLogService.GetAllUserActivityLogsAsync(filter);
                return Ok(new ResponseDto
                {
                    Status = 200,
                    ResponseMsg = "Restaurant activity logs retrieved successfully.",
                    Data = logs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto
                {
                    Status = 500,
                    ResponseMsg = ex.Message,
                    Data = null
                });
            }
        }

        [HttpGet("GetUserActivityLog/{id}")]
        public async Task<IActionResult> GetUserActivityLog(int id)
        {
            try
            {
                var log = await _activityLogService.GetUserActivityLogByIdAsync(id);
                if (log == null)
                {
                    return NotFound(new ResponseDto
                    {
                        Status = 404,
                        ResponseMsg = "Restaurant activity log not found.",
                        Data = null
                    });
                }

                return Ok(new ResponseDto
                {
                    Status = 200,
                    ResponseMsg = "Restaurant activity log retrieved successfully.",
                    Data = log
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto
                {
                    Status = 500,
                    ResponseMsg = ex.Message,
                    Data = null
                });
            }
        }

        [HttpGet("GetVendorActivityLogs")]
        public async Task<IActionResult> GetVendorActivityLogs([FromQuery] FilterActivityLogDTO filter)
        {
            try
            {
                var logs = await _vendorActivityLogService.GetAllVendorActivityLogsAsync(filter);
                return Ok(new ResponseDto
                {
                    Status = 200,
                    ResponseMsg = "Vendor activity logs retrieved successfully.",
                    Data = logs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto
                {
                    Status = 500,
                    ResponseMsg = ex.Message,
                    Data = null
                });
            }
        }

        [HttpGet("GetVendorActivityLog/{id}")]
        public async Task<IActionResult> GetVendorActivityLog(int id)
        {
            try
            {
                var log = await _vendorActivityLogService.GetVendorActivityLogByIdAsync(id);
                if (log == null)
                {
                    return NotFound(new ResponseDto
                    {
                        Status = 404,
                        ResponseMsg = "Vendor activity log not found.",
                        Data = null
                    });
                }

                return Ok(new ResponseDto
                {
                    Status = 200,
                    ResponseMsg = "Vendor activity log retrieved successfully.",
                    Data = log
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto
                {
                    Status = 500,
                    ResponseMsg = ex.Message,
                    Data = null
                });
            }
        }

        [HttpGet("GetUserEndpoints")]
        public async Task<IActionResult> GetUserEndpoints()
        {
            try
            {
                var endpoints = await _activityLogService.GetRestaurantEndpointsAsync();
                return Ok(new ResponseDto
                {
                    Status = 200,
                    ResponseMsg = "Restaurant endpoints retrieved successfully.",
                    Data = endpoints
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto
                {
                    Status = 500,
                    ResponseMsg = ex.Message,
                    Data = null
                });
            }
        }

        [HttpGet("GetVendorEndpoints")]
        public async Task<IActionResult> GetVendorEndpoints()
        {
            try
            {
                var endpoints = await _vendorActivityLogService.GetVendorEndpointsAsync();
                return Ok(new ResponseDto
                {
                    Status = 200,
                    ResponseMsg = "Vendor endpoints retrieved successfully.",
                    Data = endpoints
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto
                {
                    Status = 500,
                    ResponseMsg = ex.Message,
                    Data = null
                });
            }
        }
    }
}
