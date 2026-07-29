using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MenuOptics.Data;
using MenuOptics.Middleware;
using MenuOptics.Services;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Register DataProtection with identical application name so the Web project can decrypt the auth cookie
builder.Services.AddDataProtection()
    .SetApplicationName("MenuOptics");

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Add services to the container.
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddSingleton<DapperContext>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddDataProtection()
    .SetApplicationName("MenuOptics")
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\MenuOptics-SharedKeys")); // same path on server


// Infrastructure services
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// Business services (one per API controller)
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IEventOnlyItemService, EventOnlyItemService>();
builder.Services.AddScoped<IIngredientService, IngredientService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IMenuEngineeringInsightsService, MenuEngineeringInsightsService>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IRestaurantService, RestaurantService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IStripeWebhookService, StripeWebhookService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<IPrivateVendorService, PrivateVendorService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();



// 1. Read JWT settings from configuration (appsettings.json) and create a key for token validation
var jwtSection = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSection["Key"]);

// Configure Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/User/Login";
        options.AccessDeniedPath = "/User/AccessDenied";
        options.Cookie.Name = "RestaurantAuthCookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
    })
// 2. JWT Bearer configure karein
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),

        //Bypass bydefault expiry of JWT Token
        ClockSkew = TimeSpan.Zero
    };


    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            var ResponseDto = new
            {
                status = 401,
                responseMsg = "Unauthorized. Please provide a valid Bearer Token.",
                data = (object)null
            };
            var jsonResponse = System.Text.Json.JsonSerializer.Serialize(ResponseDto);
            await context.Response.WriteAsync(jsonResponse);
        },

        // Optional: if user is authenticated but does not have required permissions, return 403 Forbidden instead of redirecting to AccessDeniedPath
        OnForbidden = async context =>
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";

            var ResponseDto = new
            {
                status = 403,
                responseMsg = "Forbidden. You do not have permission to access this resource.",
                data = (object)null
            };

            var jsonResponse = System.Text.Json.JsonSerializer.Serialize(ResponseDto);
            await context.Response.WriteAsync(jsonResponse);
        }
    };
    // -------------------------------
});


// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Restaurant API", Version = "v1" });
});

var allowedOrigins = builder.Configuration
    .GetSection("WebBaseUrl")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Required for cookie-based auth
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");       // Must be before UseStaticFiles so /uploads/ images get CORS headers

app.UseStaticFiles();
app.UseRouting();
app.UseRequestId();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();