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
    }

    public static class ApiProxyMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiProxy(this IApplicationBuilder app)
            => app.UseMiddleware<ApiProxyMiddleware>();
    }
}
