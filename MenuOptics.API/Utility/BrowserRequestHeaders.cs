namespace MenuOptics.API.Utility
{
    public class BrowserRequestHeaders
    {
        public static string? GetClientIpAddress(HttpContext context)
        {
            // 1. Explicit client IP forwarded by Web ApiProxyMiddleware (cannot be overwritten by Cloudflare/IIS reverse proxy)
            var originalClientIp = context.Request.Headers["X-Original-Client-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(originalClientIp))
                return CleanIp(originalClientIp);

            var browserIp = context.Request.Headers["X-Browser-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(browserIp))
                return CleanIp(browserIp);

            // 2. Direct external visitor headers (e.g. direct calls to API without Web proxy)
            var cfIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(cfIp))
                return CleanIp(cfIp);

            var trueClientIp = context.Request.Headers["True-Client-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(trueClientIp))
                return CleanIp(trueClientIp);

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(realIp))
                return CleanIp(realIp);

            var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                var firstIp = forwarded.Split(',')[0].Trim();
                if (!string.IsNullOrWhiteSpace(firstIp))
                    return CleanIp(firstIp);
            }

            // 3. Fallback to connection remote IP
            var remoteIp = context.Connection.RemoteIpAddress;
            if (remoteIp != null)
            {
                if (remoteIp.IsIPv4MappedToIPv6)
                    return CleanIp(remoteIp.MapToIPv4().ToString());
                return CleanIp(remoteIp.ToString());
            }

            return null;
        }

        private static string CleanIp(string ip)
        {
            ip = ip.Trim();
            if (ip == "::1")
                return "127.0.0.1";
            // Handle IPv4:port format (e.g. 192.168.1.1:5000)
            if (!ip.StartsWith("[") && ip.Contains(':') && ip.IndexOf(':') == ip.LastIndexOf(':'))
            {
                ip = ip.Substring(0, ip.IndexOf(':'));
            }
            return ip;
        }
    }
}

