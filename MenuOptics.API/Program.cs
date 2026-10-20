using MenuOptics.API.Middleware;
using MenuOptics.Data;
using MenuOptics.Services;
using MenuOptics.Services.VendorApi;
using MenuOptics.Services.VendorApiServices.Implementations;
using MenuOptics.Services.VendorApiServices.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers to capture client IP behind reverse proxy / web proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Add services to the container.
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddSingleton<DapperContext>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

// Infrastructure services
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IVendorActivityLogService, VendorActivityLogService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ISmtpEmailService, SmtpEmailService>();

// Business services (one per API controller)
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IEventOnlyItemService, EventOnlyItemService>();
builder.Services.AddScoped<MenuOptics.Services.VendorApi.IIngredientService, MenuOptics.Services.VendorApi.IngredientService>();
builder.Services.AddScoped<MenuOptics.Services.VendorApi.IInventoryService, MenuOptics.Services.VendorApi.InventoryService>();
builder.Services.AddScoped<MenuOptics.Services.IIngredientService, MenuOptics.Services.IngredientService>();
builder.Services.AddScoped<MenuOptics.Services.IInventoryService, MenuOptics.Services.InventoryService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IMenuEngineeringInsightsService, MenuEngineeringInsightsService>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IInventorySuiteReportService, InventorySuiteReportService>();
builder.Services.AddScoped<IRestaurantService, RestaurantService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IStripeWebhookService, StripeWebhookService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPrivateVendorService, PrivateVendorService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IMenuAiAnalysisService, MenuAiAnalysisService>();

// Read JWT settings from configuration (appsettings.json) and create key for token validation
var jwtSection = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured."));

// Configure JWT Bearer as the default and only authentication scheme
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
        IssuerSigningKey = new SymmetricSecurityKey(key),
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
});

// Configure Swagger with JWT Bearer support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Restaurant API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
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

app.UseForwardedHeaders();

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

app.UseAuthentication();
app.UseMiddleware<ActivityLogMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();