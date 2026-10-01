namespace MenuOptics.API.Utility
{
    public class BrowserRequestHeaders
    {
        public static string? GetClientIpAddress(HttpContext context)
        {
            // X-Forwarded-For: real IP when behind a reverse proxy / load balancer
            var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
                return forwarded.Split(',')[0].Trim();

            return context.Connection.RemoteIpAddress?.ToString();
        }
    }
}
