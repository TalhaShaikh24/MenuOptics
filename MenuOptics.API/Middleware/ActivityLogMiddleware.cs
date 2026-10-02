using MenuOptics.API.Utility;
using MenuOptics.Services;
using MenuOptics.Services.VendorApiServices.Interfaces;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;

namespace MenuOptics.API.Middleware
{
    public class ActivityLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ActivityLogMiddleware> _logger;

        public ActivityLogMiddleware(
            RequestDelegate next,
            IConfiguration configuration,
            ILogger<ActivityLogMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IActivityLogService activityLogService)
        {
            // =========================================================
            // 1. CHECK EXCLUDED CONTROLLER
            // =========================================================

            var endpoint = context.GetEndpoint();

            var controllerAction =
                endpoint?
                    .Metadata
                    .GetMetadata<ControllerActionDescriptor>();

            var controllerNamespace =
                controllerAction?
                    .ControllerTypeInfo
                    .Namespace;

            var excludedNamespaces =
                _configuration
                    .GetSection(
                        "ActivityLog:ExcludedControllerNamespaces")
                    .Get<string[]>() ?? [];

            if (!string.IsNullOrWhiteSpace(controllerNamespace) &&
                excludedNamespaces.Any(excluded =>
                    controllerNamespace.StartsWith(
                        excluded,
                        StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            // =========================================================
            // 2. SKIP VENDOR USERS
            // =========================================================

            var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.Equals(
                    userRole,
                    "VENDOR",
                    StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // =========================================================
            // 3. SKIP GET / HEAD / OPTIONS — only log mutating requests
            // =========================================================

            var httpMethod = context.Request.Method.ToUpperInvariant();

            if (httpMethod == "GET" ||
                httpMethod == "HEAD" ||
                httpMethod == "OPTIONS")
            {
                await _next(context);
                return;
            }

            // =========================================================
            // 4. REQUEST INFORMATION
            // =========================================================

            var requestId = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant();
            var ipAddress = BrowserRequestHeaders.GetClientIpAddress(context);

            // Use format: "HTTP_METHOD ActionName" e.g. "POST CreateCategory"
            var actionMethodName = controllerAction?.MethodInfo?.Name;

            var requestType = !string.IsNullOrWhiteSpace(actionMethodName)
                ? $"{context.Request.Method.ToUpperInvariant()} {context.Request.Path}"
                : $"{context.Request.Method.ToUpperInvariant()} {context.Request.Path}";

            string? requestBody = null;

            // =========================================================
            // 6. READ REQUEST BODY
            // =========================================================

            if (context.Request.ContentLength > 0 &&
                context.Request.ContentType?.Contains(
                    "application/json",
                    StringComparison.OrdinalIgnoreCase) == true)
            {
                context.Request.EnableBuffering();

                using var reader = new StreamReader(
                    context.Request.Body,
                    Encoding.UTF8,
                    leaveOpen: true);

                requestBody = await reader.ReadToEndAsync();

                context.Request.Body.Position = 0;
            }

            // =========================================================
            // 6. CAPTURE RESPONSE
            // =========================================================

            var originalResponseBody = context.Response.Body;

            await using var responseBody = new MemoryStream();

            context.Response.Body = responseBody;

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // =====================================================
                // 7. RUN ACTUAL APPLICATION
                // =====================================================

                await _next(context);

                stopwatch.Stop();

                // =====================================================
                // 8. READ RESPONSE
                // =====================================================

                context.Response.Body.Position = 0;

                string? responseContent = null;

                if (context.Response.ContentType?.Contains(
                        "application/json",
                        StringComparison.OrdinalIgnoreCase) == true)
                {
                    using var reader = new StreamReader(
                        context.Response.Body,
                        Encoding.UTF8,
                        leaveOpen: true);

                    responseContent =
                        await reader.ReadToEndAsync();

                    context.Response.Body.Position = 0;
                }

                // =====================================================
                // 9. GET USER ID & IP ADDRESS
                // =====================================================

                var userId = GetUserId(context);

                // =====================================================
                // 10. DETERMINE LOG LEVEL
                // =====================================================

                var statusCode = context.Response.StatusCode;
                var logLevel = (statusCode == 200 || statusCode == 201)
                    ? "Success"
                    : "Error";

                // =====================================================
                // 11. SAVE ACTIVITY LOG
                // =====================================================

                await activityLogService.LogAsync(
                    userId,
                    requestId,
                    requestType,
                    requestBody,
                    responseContent,
                    logLevel,
                    ipAddress);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                var userId = GetUserId(context);

                // =====================================================
                // 12. LOG EXCEPTION
                // =====================================================

                await activityLogService.LogAsync(
                    userId,
                    requestId,
                    requestType,
                    requestBody,
                    ex.Message,
                    "ERROR",
                    ipAddress);

                throw;
            }
            finally
            {
                // =====================================================
                // 13. SEND ORIGINAL RESPONSE TO CLIENT
                // =====================================================

                context.Response.Body.Position = 0;

                await responseBody.CopyToAsync(
                    originalResponseBody);

                context.Response.Body =
                    originalResponseBody;
            }
        }

        private static int? GetUserId(
            HttpContext context)
        {
            var userIdValue = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(
                userIdValue,
                out var userId))
            {
                return userId;
            }

            return null;
        }

       
    }
}