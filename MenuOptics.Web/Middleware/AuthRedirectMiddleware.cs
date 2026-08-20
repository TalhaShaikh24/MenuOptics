namespace MenuOptics.Middleware
{
    public class AuthRedirectMiddleware
    {
        private readonly RequestDelegate _next;

        // Ye paths bina login ke allow hain
        private static readonly HashSet<string> _publicPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/",
            "/Home",
            "/Home/Index",
            "/User/Login",
            "/User/Logout",
            "/User/AccessDenied",
            "/UserApi/Login",
            "/api/UserApi/Login",
            "/api/UserApi/CreateUser",
            "/api/UserApi/CreateCheckoutSession",
            "/User/Register",
            "/Payment/Cancel",
            "/Payment/Success",
            "/Home/TermsAndConditions",
            "/Home/Privacy",
            "/Home/Faq",
        };

        public AuthRedirectMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // API calls, static files aur public paths skip karo
            bool isApi = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
            bool isStatic = path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)
                         || path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase)
                         || path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase)
                         || path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase)
                         || path.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)
                         || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);
            bool isPublic = _publicPaths.Contains(path);

            if (!isApi && !isStatic && !isPublic && !context.User.Identity!.IsAuthenticated)
            {
                context.Response.Redirect("/User/Login");
                return;
            }

            await _next(context);
        }
    }

    public static class AuthRedirectMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthRedirect(this IApplicationBuilder app)
            => app.UseMiddleware<AuthRedirectMiddleware>();
    }
}