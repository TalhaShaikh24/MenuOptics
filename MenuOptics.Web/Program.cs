using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.DataProtection;
using MenuOptics.Middleware;
using MenuOptics.Web.Middleware;

using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers to capture client IP behind reverse proxy / IIS / Cloudflare
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Register DataProtection with matching application name to decrypt the auth cookie
builder.Services.AddDataProtection()
    .SetApplicationName("MenuOptics");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddDataProtection()
    .SetApplicationName("MenuOptics")
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\MenuOptics-SharedKeys")); // same path on server


// Register the HttpClient named Client for proxying
builder.Services.AddHttpClient("ApiClient", client =>
{
    // Configuration here if needed, e.g. Timeout
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = false
});

// Configure Cookie Authentication to match API's cookie setup
var expiryHours = double.TryParse(builder.Configuration["Jwt:ExpiryInHours"], out var hours) && hours > 0 ? hours : 1;
var cookieExpiry = TimeSpan.FromHours(expiryHours);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/User/Login?sessionExpired=true";
        options.AccessDeniedPath = "/User/AccessDenied";
        options.Cookie.Name = "RestaurantAuthCookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = cookieExpiry;
    });

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// 1. Intercept and proxy all API calls before any other MVC pipeline logic
app.UseApiProxy();

// 2. Serve static files (css, js, images, uploads)
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAuthRedirect();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
