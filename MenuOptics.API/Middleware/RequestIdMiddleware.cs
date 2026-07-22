namespace MenuOptics.Middleware
{
    public class RequestIdMiddleware
    {
        // The five endpoints that require request-id + activity logging
        private static readonly HashSet<string> _auditedPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/api/UserApi/Login",
            "/api/IngredientApi/CreateIngredientByVendor",
            "/api/IngredientApi/UpdateIngredientPrices",
            "/api/InventoryApi/InvoiceInventory",
            "/api/IngredientApi/PatchIngredientDetails"
        };

        public const string RequestIdItemKey = "X-Request-Id";

        private readonly RequestDelegate _next;

        public RequestIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            if (_auditedPaths.Contains(path))
            {
                // Accept caller-supplied id or generate a fresh one
                var requestId = context.Request.Headers[RequestIdItemKey].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(requestId))
                {
                    requestId = Guid.NewGuid().ToString();
                }

                // Expose on request header (so controllers can read via Request.Headers)
                context.Request.Headers[RequestIdItemKey] = requestId;

                // Expose via Items (faster lookup inside the same request)
                context.Items[RequestIdItemKey] = requestId;

                // Echo back in ResponseDto header
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers[RequestIdItemKey] = requestId;
                    return Task.CompletedTask;
                });
            }

            await _next(context);
        }
    }

    // Extension method for clean registration in Program.cs
    public static class RequestIdMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestId(this IApplicationBuilder app)
            => app.UseMiddleware<RequestIdMiddleware>();
    }
}