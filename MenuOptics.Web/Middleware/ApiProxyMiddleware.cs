using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MenuOptics.Web.Middleware
{
    public class ApiProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public ApiProxyMiddleware(RequestDelegate next, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _next = next;
            _httpClient = httpClientFactory.CreateClient("ApiClient");
            _apiBaseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:7104/";
            if (!_apiBaseUrl.EndsWith("/"))
            {
                _apiBaseUrl += "/";
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            bool isApi = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
            if (isApi)
            {
                // Proxy the request to the API
                var relativePath = path.Substring(5); // Remove "/api/"
                var query = context.Request.QueryString.Value ?? string.Empty;
                var targetUrl = $"{_apiBaseUrl}api/{relativePath}{query}";

                var requestMessage = new HttpRequestMessage();
                
                // Copy body if present
                if (HttpMethods.IsPost(context.Request.Method) || 
                    HttpMethods.IsPut(context.Request.Method) || 
                    HttpMethods.IsPatch(context.Request.Method) || 
                    HttpMethods.IsDelete(context.Request.Method))
                {
                    var requestStream = new MemoryStream();
                    await context.Request.Body.CopyToAsync(requestStream);
                    requestStream.Position = 0;
                    requestMessage.Content = new StreamContent(requestStream);
                    
                    if (!string.IsNullOrEmpty(context.Request.ContentType))
                    {
                        requestMessage.Content.Headers.ContentType = 
                            System.Net.Http.Headers.MediaTypeHeaderValue.Parse(context.Request.ContentType);
                    }
                }

                requestMessage.Method = new HttpMethod(context.Request.Method);
                requestMessage.RequestUri = new Uri(targetUrl);

                // Copy request headers
                foreach (var header in context.Request.Headers)
                {
                    if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Content-Type and Content-Length are set on requestMessage.Content.Headers
                    if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) || 
                        header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                        continue;

                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }

                // Forward real client IP from browser/user to API using custom headers
                // Custom headers like X-Original-Client-IP will NEVER be overwritten by Cloudflare or IIS reverse proxies
                var clientIp = GetClientIp(context);
                if (!string.IsNullOrWhiteSpace(clientIp))
                {
                    requestMessage.Headers.Remove("X-Original-Client-IP");
                    requestMessage.Headers.TryAddWithoutValidation("X-Original-Client-IP", clientIp);

                    requestMessage.Headers.Remove("X-Browser-IP");
                    requestMessage.Headers.TryAddWithoutValidation("X-Browser-IP", clientIp);

                    requestMessage.Headers.Remove("X-Forwarded-For");
                    requestMessage.Headers.TryAddWithoutValidation("X-Forwarded-For", clientIp);

                    requestMessage.Headers.Remove("X-Real-IP");
                    requestMessage.Headers.TryAddWithoutValidation("X-Real-IP", clientIp);
                }

                // Forward scheme / protocol (http vs https)
                requestMessage.Headers.Remove("X-Forwarded-Proto");
                requestMessage.Headers.TryAddWithoutValidation("X-Forwarded-Proto", context.Request.Scheme);

                try
                {
                    var responseMessage = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
                    
                    context.Response.StatusCode = (int)responseMessage.StatusCode;

                    // Copy ResponseDto headers
                    foreach (var header in responseMessage.Headers)
                    {
                        context.Response.Headers[header.Key] = header.Value.ToArray();
                    }
                    foreach (var header in responseMessage.Content.Headers)
                    {
                        if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) || 
                            header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Response.Headers[header.Key] = header.Value.ToArray();
                        }
                    }

                    // Remove transfer-encoding chunked to avoid protocol errors if content-length is set
                    context.Response.Headers.Remove("transfer-encoding");

                    await responseMessage.Content.CopyToAsync(context.Response.Body);
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = StatusCodes.Status502BadGateway;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new { 
                        status = 502, 
                        responseMsg = $"Bad Gateway: Could not connect to API service. {ex.Message}", 
                        data = (object)null 
                    });
                }

                return;
            }

            await _next(context);
        }

        private static string? GetClientIp(HttpContext context)
        {
            // 1. Cloudflare header
            var cfIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(cfIp))
                return CleanIp(cfIp);

            // 2. True-Client-IP
            var trueClientIp = context.Request.Headers["True-Client-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(trueClientIp))
                return CleanIp(trueClientIp);

            // 3. X-Real-IP
            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(realIp))
                return CleanIp(realIp);

            // 4. X-Forwarded-For (take the first IP in the list)
            var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                var firstIp = forwarded.Split(',')[0].Trim();
                if (!string.IsNullOrWhiteSpace(firstIp))
                    return CleanIp(firstIp);
            }

            // 5. RemoteIpAddress fallback
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

    public static class ApiProxyMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiProxy(this IApplicationBuilder app)
            => app.UseMiddleware<ApiProxyMiddleware>();
    }
}
