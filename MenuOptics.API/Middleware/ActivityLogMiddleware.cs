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
            // 2. SKIP GET / HEAD / OPTIONS — only log mutating requests
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
            // 3. REQUEST INFORMATION
            // =========================================================

            var requestId = context.TraceIdentifier;

            // Use format: "HTTP_METHOD ActionName" e.g. "POST CreateCategory"
            var actionMethodName = controllerAction?.MethodInfo?.Name;

            var requestType = !string.IsNullOrWhiteSpace(actionMethodName)
                ? $"{context.Request.Method.ToUpperInvariant()} {actionMethodName}"
                : $"{context.Request.Method.ToUpperInvariant()} {context.Request.Path}";

            string? requestBody = null;

            // =========================================================
            // 3. READ REQUEST BODY
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
            // 4. CAPTURE RESPONSE
            // =========================================================

            var originalResponseBody = context.Response.Body;

            await using var responseBody = new MemoryStream();

            context.Response.Body = responseBody;

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // =====================================================
                // 5. RUN ACTUAL APPLICATION
                // =====================================================

                await _next(context);

                stopwatch.Stop();

                // =====================================================
                // 6. READ RESPONSE
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
                // 7. GET USER ID & IP ADDRESS
                // =====================================================

                var userId = GetUserId(context);
                var ipAddress = GetClientIpAddress(context);

                // =====================================================
                // 8. DETERMINE LOG LEVEL
                // =====================================================

                var statusCode = context.Response.StatusCode;
                var logLevel = (statusCode == 200 || statusCode == 201)
                    ? "SUCCESS"
                    : "ERROR";

                // =====================================================
                // 9. SAVE ACTIVITY LOG
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
                var ipAddress = GetClientIpAddress(context);

                // =====================================================
                // 10. LOG EXCEPTION
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
                // 11. SEND ORIGINAL RESPONSE TO CLIENT
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

            if (int.TryParse(userIdValue, out var userId))
                return userId;

            return null;
        }

        private static string? GetClientIpAddress(HttpContext context)
        {
            // X-Forwarded-For: real IP when behind a reverse proxy / load balancer
            var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
                return forwarded.Split(',')[0].Trim();

            return context.Connection.RemoteIpAddress?.ToString();
        }
    }
}