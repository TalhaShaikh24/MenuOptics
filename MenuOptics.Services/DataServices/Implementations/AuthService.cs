using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using MenuOptics.Models.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Stripe;
using Stripe.Checkout;
using System.Data;
using System.Data.SqlClient;
using System.Security.Claims;

namespace MenuOptics.Services
{
    public class AuthService : IAuthService
    {
        private readonly DapperContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IActivityLogService _activityLog;
        private readonly ITokenService _tokenService;
        private readonly ISmtpEmailService _emailService;
        private readonly IFileUploadService _fileService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public AuthService(
            DapperContext context,
            IWebHostEnvironment environment,
            IActivityLogService activityLog,
            ITokenService tokenService,
            ISmtpEmailService emailService,
            IFileUploadService fileService,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _activityLog = activityLog;
            _tokenService = tokenService;
            _emailService = emailService;
            _fileService = fileService;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public async Task<ResponseDto> LoginAsync(LoginDto model, string requestId)
        {
            var ResponseDto = new ResponseDto();
            var payload = System.Text.Json.JsonSerializer.Serialize(new { model.Username }); // never log password

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Username", model.Username);
                parameters.Add("@Action", "LOGIN");

                var user = await connection.QueryFirstOrDefaultAsync<UserDto>(
                    "sp_Auth", parameters, commandType: CommandType.StoredProcedure);


                if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    ResponseDto.Status = 401;
                    ResponseDto.ResponseMsg = "Invalid password.";
                    return ResponseDto;
                }

                var effectiveUserId = user.ParentId.HasValue ? user.ParentId.Value : user.UserId;
                var isParent = !user.ParentId.HasValue;

                await SignInWithCookieAsync(user, effectiveUserId, isParent);

                var tokenString = _tokenService.GenerateAccessToken(user);
                var refreshTokenString = _tokenService.GenerateRefreshToken();
                await _tokenService.SaveRefreshTokenAsync(user.UserId, refreshTokenString);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Login successful.";
                ResponseDto.Data = new
                {
                    UserId = effectiveUserId,
                    RestaurantName = user.RestaurantName,
                    Phone = user.Phone,
                    Address = user.Address,
                    ActualUserId = user.UserId,
                    IsParent = isParent,
                    Roles = user.Roles,
                    ProfilePic = user.ProfilePic,
                    Token = tokenString,
                    RefreshToken = refreshTokenString,
                    PaidBy = user.PaidBy,
                    HasViewedIntroVideo = user.HasViewedIntroVideo,
                };

                //Keep TOGGLE_INTRO_VIDEO_VIEWED part after the ResponseDto is prepared
                if (user.HasViewedIntroVideo == false)
                {
                    var updateParams = new DynamicParameters();
                    updateParams.Add("@UserId", user.UserId);
                    updateParams.Add("@HasViewedIntroVideo", true);
                    updateParams.Add("@Action", "TOGGLE_INTRO_VIDEO_VIEWED");
                    await connection.ExecuteAsync("sp_UserCRUD", updateParams, commandType: CommandType.StoredProcedure);
                }

                if (user.Roles?.Contains(Role.Vendor.ToString()) == true)
                {
                    await _activityLog.LogAsync(user.UserId, requestId, "POST /api/UserApi/Login", payload,
                        System.Text.Json.JsonSerializer.Serialize(new { ResponseDto.Status, ResponseDto.ResponseMsg }), "Success");
                }

                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> LogoutAsync()
        {
            var ResponseDto = new ResponseDto();
            await _httpContextAccessor.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            ResponseDto.Status = 200;
            ResponseDto.ResponseMsg = "Logout successful.";
            ResponseDto.Data = null;
            return ResponseDto;
        }

        public (string SessionId, string? Url) CreateCheckoutSession(string? plan, int userId)
        {
            var stripeKey = _configuration["Stripe:SecretKey"];
            StripeConfiguration.ApiKey = stripeKey;

            var starterPrice = _configuration["Stripe:StarterPriceId"];
            var proPrice = _configuration["Stripe:ProfessionalPriceId"];

            var priceId = (plan?.ToLower()) switch
            {
                "starter" => starterPrice,
                _ => proPrice
            };

            // Dynamically resolve client Web Base URL from Origin or Referer header
            var httpContext = _httpContextAccessor.HttpContext;
            string? origin = httpContext?.Request.Headers["Origin"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(origin))
            {
                var referer = httpContext?.Request.Headers["Referer"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var uri))
                {
                    origin = $"{uri.Scheme}://{uri.Authority}";
                }
            }

            var baseUrl = !string.IsNullOrWhiteSpace(origin)
                ? origin.TrimEnd('/')
                : (_configuration["WebUrl"] ?? "https://localhost:7103").TrimEnd('/');

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "subscription",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1,
                    },
                },
                Metadata = new Dictionary<string, string>
                {
                    { "UserId", userId.ToString() }
                },
                SuccessUrl = $"{baseUrl}/Payment/successpayment?session_id={{CHECKOUT_SESSION_ID}}&user_id={userId}",
                CancelUrl = $"{baseUrl}/Payment/Cancel",
            };

            var service = new SessionService();
            var session = service.Create(options);

            return (session.Id, session.Url);
        }

        public async Task<ResponseDto> RefreshTokenAsync(RefreshTokenRequestDto model)
        {
            var ResponseDto = new ResponseDto();

            if (string.IsNullOrWhiteSpace(model?.RefreshToken))
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = "Refresh token is required.";
                return ResponseDto;
            }

            try
            {
                // Validate the refresh token from DB
                var storedToken = await _tokenService.GetRefreshTokenAsync(model.RefreshToken);

                if (storedToken == null)
                {
                    ResponseDto.Status = 401;
                    ResponseDto.ResponseMsg = "Invalid refresh token.";
                    return ResponseDto;
                }

                if (storedToken.IsRevoked)
                {
                    ResponseDto.Status = 401;
                    ResponseDto.ResponseMsg = "Refresh token has been revoked.";
                    return ResponseDto;
                }

                if (storedToken.ExpiresAt < DateTime.UtcNow)
                {
                    ResponseDto.Status = 401;
                    ResponseDto.ResponseMsg = "Refresh token has expired. Please log in again.";
                    return ResponseDto;
                }

                // Fetch user data to build a fresh access token
                using var connection = _context.CreateConnection();
                connection.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", storedToken.UserId);
                parameters.Add("@Action", "SELECT");

                var user = await connection.QueryFirstOrDefaultAsync<UserDto>(
                    "sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (user == null || !user.IsActive)
                {
                    ResponseDto.Status = 401;
                    ResponseDto.ResponseMsg = "User not found or account is inactive.";
                    return ResponseDto;
                }

                // Rotate tokens: revoke old, issue new pair
                await _tokenService.RevokeRefreshTokenAsync(model.RefreshToken);

                var newAccessToken = _tokenService.GenerateAccessToken(user);
                var newRefreshToken = _tokenService.GenerateRefreshToken();
                await _tokenService.SaveRefreshTokenAsync(user.UserId, newRefreshToken);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Token refreshed successfully.";
                ResponseDto.Data = new
                {
                    Token = newAccessToken,
                    RefreshToken = newRefreshToken
                };

                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
        }

        public async Task<ResponseDto> RevokeTokenAsync(RefreshTokenRequestDto model)
        {
            var ResponseDto = new ResponseDto();

            if (string.IsNullOrWhiteSpace(model?.RefreshToken))
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = "Refresh token is required.";
                return ResponseDto;
            }

            try
            {
                var storedToken = await _tokenService.GetRefreshTokenAsync(model.RefreshToken);

                if (storedToken == null || storedToken.IsRevoked)
                {
                    ResponseDto.Status = 400;
                    ResponseDto.ResponseMsg = "Token is already invalid or does not exist.";
                    return ResponseDto;
                }

                await _tokenService.RevokeRefreshTokenAsync(model.RefreshToken);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Token revoked successfully.";
                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
        }

        public async Task<ResponseDto> CheckUserAsync(CheckUserDto model)
        {
            var ResponseDto = new ResponseDto();

            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Username", model.Email);
                parameters.Add("@Action", "CHECK_USER");

                // Returns: UserId, Email, IsRegistrationCompleted
                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "sp_Auth", parameters, commandType: CommandType.StoredProcedure);


                // Nullable: treat NULL as not completed (first-time member)
                bool isCompleted = result.IsRegistrationCompleted == true;

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Account found.";
                ResponseDto.Data = new
                {
                    UserId = (int)result.UserId,
                    Username = (string)result.Username,
                    IsRegistrationCompleted = isCompleted
                };
                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> SendOtpAsync(SendOtpDto model)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                // 1. Load the user's email address
                var selectParams = new DynamicParameters();
                selectParams.Add("@UserId", model.UserId);
                selectParams.Add("@Action", "SELECT");

                var user = await connection.QueryFirstOrDefaultAsync<UserDto>(
                    "sp_UserCRUD", selectParams, commandType: CommandType.StoredProcedure);

                
                // 2. Generate 6-digit OTP
                var otp = new Random().Next(100000, 999999).ToString();

                // 3. Persist OTP in DB via sp_OtpCRUD
                var otpParams = new DynamicParameters();
                otpParams.Add("@UserId", model.UserId);
                otpParams.Add("@OtpCode", otp);
                otpParams.Add("@Action", "INSERT_OTP");

                await connection.ExecuteScalarAsync<int>(
                    "sp_OtpCRUD", otpParams, commandType: CommandType.StoredProcedure);

                // 4. Send OTP via email
                var subject = "Your Verification Code";
                var body = $@"
                        <p>Hello,</p>
                        <p>Use the code below to verify your email and complete your account setup.
                           This code expires in <strong>10 minutes</strong>.</p>
                        <h2 style='letter-spacing:4px;'>{otp}</h2>
                        <p>If you did not request this, please ignore this email.</p>";

                await _emailService.SendEmailAsync(user.Email!, subject, body);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "OTP sent to your email.";
                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> VerifyOtpAsync(VerifyOtpDto model)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var otpParams = new DynamicParameters();
                otpParams.Add("@UserId", model.UserId);
                otpParams.Add("@OtpCode", model.Otp);
                otpParams.Add("@Action", "VERIFY_OTP");

                // SP returns a single column: IsValid (1 or 0)
                var isValid = await connection.ExecuteScalarAsync<int>(
                    "sp_OtpCRUD", otpParams, commandType: CommandType.StoredProcedure);

                if (isValid == 1)
                {
                    ResponseDto.Status = 200;
                    ResponseDto.ResponseMsg = "OTP verified successfully.";
                    ResponseDto.Data = new { IsValid = true };
                    return ResponseDto;
                }

                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = "Invalid or expired OTP.";
                ResponseDto.Data = new { IsValid = false };
                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task SignInWithCookieAsync(UserDto user, int effectiveUserId, bool isParent)
        {
            var stripeSubscriptionId = user.StripeSubscriptionId ?? "";
            var subscriptionType = user.SubscriptionType ?? "";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                // CurrentUserId is the ID of the user whose data is being accessed, which may be different from effectiveUserId if the user is a child account.
                new Claim(ClaimTypes.NameIdentifier, effectiveUserId.ToString()),
                new Claim(ClaimTypes.Role, user.Roles ?? ""),
                new Claim("FirstName", user.FirstName ?? ""),
                new Claim("LastName", user.LastName ?? ""),
                // ActualUserId is the ID of the user who logged in, which may be different from effectiveUserId if the user is a child account.
                new Claim("ActualUserId", user.UserId.ToString()),
                new Claim("IsParent", isParent.ToString()),
                new Claim("StripeSubscriptionId", stripeSubscriptionId.ToString()),
                new Claim("SubscriptionType", subscriptionType.ToString().ToUpper()),
                new Claim("PaidBy", user.PaidBy?.ToString() ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
            };

            await _httpContextAccessor.HttpContext!.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }
    }
}
