using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MenuOptics.Data;
using MenuOptics.Models;
using Stripe;
using Stripe.Checkout;
using System.Data;
using System.Data.SqlClient;
using System.Security.Claims;

namespace MenuOptics.Services
{
    public class UserService : IUserService
    {
        private readonly DapperContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IActivityLogService _activityLog;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IFileUploadService _fileService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(
            DapperContext context,
            IWebHostEnvironment environment,
            IActivityLogService activityLog,
            ITokenService tokenService,
            IEmailService emailService,
            IFileUploadService fileService,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _environment = environment;
            _activityLog = activityLog;
            _tokenService = tokenService;
            _emailService = emailService;
            _fileService = fileService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ResponseDto> GetUserStatsAsync()
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_USER_STATS");

                var stats = await connection.QueryFirstOrDefaultAsync<UserStatsDto>(
                    "sp_UserCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                if (stats == null)
                {
                    ResponseDto.Status = 404;
                    ResponseDto.ResponseMsg = "No user stats available.";
                    return ResponseDto;
                }

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "User stats retrieved successfully.";
                ResponseDto.Data = stats;
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

        public async Task<ResponseDto> GetRestaurantStatsAsync()
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_RESTAURANT_STATS");

                var stats = await connection.QueryFirstOrDefaultAsync<UserStatsDto>(
                    "sp_UserCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                if (stats == null)
                {
                    ResponseDto.Status = 404;
                    ResponseDto.ResponseMsg = "No restaurant stats available.";
                    return ResponseDto;
                }

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Restaurant stats retrieved successfully.";
                ResponseDto.Data = stats;
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

        public async Task<ResponseDto> CreateUserAsync(User model, int fallbackCreatedBy, string requestBaseUrl)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                string filePathUrl = null;
                if (model.File != null)
                {
                    filePathUrl = await _fileService.UploadFileAsync(model.File, "uploads");
                }

                var parameters = new DynamicParameters();
                parameters.Add("@FirstName", model.FirstName);
                parameters.Add("@LastName", model.LastName);
                parameters.Add("@Username", model.Username);
                parameters.Add("@Email", model.Email);
                parameters.Add("@Phone", model.Phone);
                parameters.Add("@ProfilePic", filePathUrl);
                parameters.Add("@Address", model.Address);
                parameters.Add("@Country", model.Country);
                parameters.Add("@State", model.State);
                parameters.Add("@City", model.City);
                parameters.Add("@Zip", model.Zip);
                parameters.Add("@Gender", model.Gender);
                parameters.Add("@RestaurantName", model.Role == "Restaurant" ? model.RestaurantName : null);
                parameters.Add("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(model.Password));
                parameters.Add("@RoleName", model.Role);
                parameters.Add("@IsRegistrationCompleted", 1);
                parameters.Add("@SubscriptionStatus", model.Role == "Vendor" ? null : "Pending");
                parameters.Add("@IsParent", model.Role == "Vendor" || model.Role == "Admin" ? null : 1);
                parameters.Add("@CreatedBy", model.CreatedBy ?? fallbackCreatedBy);
                parameters.Add("@Action", "INSERT");

                int userId = await connection.ExecuteScalarAsync<int>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "User created successfully.";
                ResponseDto.Data = new { UserId = userId };
                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = $"{sqlEx.Message}";
                return ResponseDto;
            }
            catch (InvalidOperationException invalidOpEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = $"{invalidOpEx.Message}";
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> GetUserAsync(int id)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", id);
                parameters.Add("@Action", "SELECT");

                var userProfile = await connection.QueryFirstOrDefaultAsync<UserDto>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (userProfile == null)
                {
                    ResponseDto.Status = 404;
                    ResponseDto.ResponseMsg = "User profile not found.";
                    return ResponseDto;
                }

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "User profile retrieved successfully.";
                ResponseDto.Data = userProfile;
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

        public async Task<ResponseDto> GetAllUsersAsync()
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL");

                var users = await connection.QueryAsync<UserDto>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Users retrieved successfully.";
                ResponseDto.Data = users;
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

        public async Task<ResponseDto> GetAllRestaurantsAsync()
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL_RESTAURANT");

                var users = await connection.QueryAsync<UserDto>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Users retrieved successfully.";
                ResponseDto.Data = users;
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

        public async Task<ResponseDto> GetAllVendorsAsync(int currentUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@Action", "SELECT_ALL_VENDORS");

                var vendors = await connection.QueryAsync<MixedVendor>("sp_Common", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Vendors retrieved successfully.";
                ResponseDto.Data = vendors;
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
        public async Task<ResponseDto> UpdateUserAsync(User model, int modifiedBy, string requestBaseUrl)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (model.UserId <= 0)
                {
                    ResponseDto.Status = 400;
                    ResponseDto.ResponseMsg = "UserId is required for updating a user.";
                    return ResponseDto;
                }

                var filePathUrl = model.ProfilePic;
                if (model.File != null)
                {
                    filePathUrl = await _fileService.UploadFileAsync(model.File, "uploads");
                }

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", model.UserId);
                parameters.Add("@FirstName", model.FirstName);
                parameters.Add("@LastName", model.LastName);
                parameters.Add("@Phone", model.Phone);
                parameters.Add("@ProfilePic", filePathUrl);
                parameters.Add("@Address", model.Address);
                parameters.Add("@Country", model.Country);
                parameters.Add("@State", model.State);
                parameters.Add("@City", model.City);
                parameters.Add("@Zip", model.Zip);
                parameters.Add("@Gender", model.Gender);
                parameters.Add("@RestaurantName", model.Role == "Restaurant" ? model.RestaurantName : null);
                parameters.Add("@IsActive", model.IsActive);
                parameters.Add("@ModifiedBy", modifiedBy);
                parameters.Add("@Action", "UPDATE");

                int userId = await connection.ExecuteScalarAsync<int>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "User updated successfully.";
                ResponseDto.Data = new { UserId = userId };
                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 503;
                ResponseDto.ResponseMsg = $"Expection: {sqlEx.Message}";
                return ResponseDto;
            }
            catch (InvalidOperationException invalidOpEx)
            {
                ResponseDto.Status = 422;
                ResponseDto.ResponseMsg = $"Invalid operation: {invalidOpEx.Message}";
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
        public async Task<ResponseDto> ToggleUserActiveAsync(int id, int currentUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", id);
                parameters.Add("@Action", "SELECT");
                var user = await connection.QueryFirstOrDefaultAsync<UserDto>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (user == null)
                {
                    ResponseDto.Status = 404;
                    ResponseDto.ResponseMsg = "User not found.";
                    return ResponseDto;
                }

                parameters = new DynamicParameters();
                parameters.Add("@UserId", id);
                parameters.Add("@IsActive", !user.IsActive);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "TOGGLE_ACTIVE");

                await connection.ExecuteAsync("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = $"User {(user.IsActive ? "deactivated" : "activated")} successfully.";
                ResponseDto.Data = new { UserId = id, IsActive = !user.IsActive };
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
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
                    "sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (user == null || user.Roles == null)
                {
                    ResponseDto.Status = 401;
                    ResponseDto.ResponseMsg = "Invalid username.";
                    return ResponseDto;
                }

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
                    IntroVideoViewedOn = user.IntroVideoViewedOn,
                };

                if (user.Roles?.Contains("Restaurant") == true && user.IntroVideoViewedOn == true)
                {
                    var updateParams = new DynamicParameters();
                    updateParams.Add("@UserId", user.UserId);
                    updateParams.Add("@IntroVideoViewedOn", 0);
                    updateParams.Add("@Action", "TOGGLE_INTRO_VIDEO_VIEWED");
                    await connection.ExecuteAsync("sp_UserCRUD", updateParams, commandType: CommandType.StoredProcedure);
                }

                if (user.Roles?.Contains("Vendor") == true)
                {
                    await _activityLog.LogAsync(user.UserId, requestId, "POST /api/UserApi/Login", payload,
                        System.Text.Json.JsonSerializer.Serialize(new { ResponseDto.Status, ResponseDto.ResponseMsg }), "Success");
                }

                return ResponseDto;
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower() == "invalidusername")
                {
                    ResponseDto.Status = 401;
                    ResponseDto.ResponseMsg = "Invalid Username";
                    return ResponseDto;
                }

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
        public async Task<ResponseDto> AddRestaurantMemberAsync(AddRestaurantMemberDto model, int currentUserId, string callerRole, string isParentClaim)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                // Restaurant owners who are members (IsParent=false) cannot create sub-members
                if (callerRole.Contains("Restaurant") && isParentClaim != "true")
                {
                    ResponseDto.Status = 403;
                    ResponseDto.ResponseMsg = "Only the parent restaurant account can add members.";
                    return ResponseDto;
                }

                int resolvedParentId;
                if (callerRole.Contains("Admin"))
                {
                    if (!model.ParentId.HasValue || model.ParentId <= 0)
                    {
                        ResponseDto.Status = 400;
                        ResponseDto.ResponseMsg = "Admin must provide a valid ParentId (the restaurant owner's UserId).";
                        return ResponseDto;
                    }
                    resolvedParentId = model.ParentId.Value;
                }
                else
                {
                    resolvedParentId = currentUserId;
                }

                var parameters = new DynamicParameters();
                parameters.Add("@Username", model.Username);
                parameters.Add("@Email", model.Email);
                parameters.Add("@Phone", model.Phone);
                parameters.Add("@FirstName", model.FirstName);
                parameters.Add("@LastName", model.LastName);
                parameters.Add("@Address", model.Address);
                parameters.Add("@Country", model.Country);
                parameters.Add("@State", model.State);
                parameters.Add("@City", model.City);
                parameters.Add("@Zip", model.Zip);
                parameters.Add("@Gender", model.Gender);
                parameters.Add("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(model.Password));
                parameters.Add("@ParentId", resolvedParentId);
                parameters.Add("@IsRegistrationCompleted", 1);
                parameters.Add("@CreatedBy", currentUserId);
                parameters.Add("@Action", "INSERT_MEMBER");

                int newMemberId = await connection.ExecuteScalarAsync<int>(
                    "sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Restaurant member added successfully.";
                ResponseDto.Data = new { MemberId = newMemberId };
                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 503;
                ResponseDto.ResponseMsg = $"{sqlEx.Message}";
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally { connection.Close(); }
        }
        public async Task<ResponseDto> GetRestaurantMembersAsync(int currentUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@ParentId", currentUserId);
                parameters.Add("@Action", "SELECT_MEMBERS");

                var members = await connection.QueryAsync<UserDto>(
                    "sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Members retrieved successfully.";
                ResponseDto.Data = members;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally { connection.Close(); }
        }
        public async Task<ResponseDto> GetMemberStatsAsync(int currentUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@ParentId", currentUserId);
                parameters.Add("@Action", "SELECT_MEMBER_STATS");

                var stats = await connection.QueryFirstOrDefaultAsync<UserStatsDto>(
                    "sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Member stats retrieved successfully.";
                ResponseDto.Data = stats;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally { connection.Close(); }
        }
        public async Task<ResponseDto> UpdateRestaurantMemberAsync(UpdateRestaurantMemberDto model, int currentUserId, string requestBaseUrl)
        {
            var ResponseDto = new ResponseDto();

            if (model.MemberId <= 0)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = "MemberId is required.";
                return ResponseDto;
            }

            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                // Verify the member belongs to this parent
                var checkParams = new DynamicParameters();
                checkParams.Add("@UserId", model.MemberId);
                checkParams.Add("@Action", "SELECT");
                var member = await connection.QueryFirstOrDefaultAsync<UserDto>(
                    "sp_UserCRUD", checkParams, commandType: CommandType.StoredProcedure);

                if (member == null || member.ParentId != currentUserId)
                {
                    ResponseDto.Status = 403;
                    ResponseDto.ResponseMsg = "Access denied: You can only update your own members.";
                    return ResponseDto;
                }

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", model.MemberId);
                parameters.Add("@Phone", model.Phone);
                parameters.Add("@FirstName", model.FirstName);
                parameters.Add("@LastName", model.LastName);
                parameters.Add("@Address", model.Address);
                parameters.Add("@Country", model.Country);
                parameters.Add("@State", model.State);
                parameters.Add("@City", model.City);
                parameters.Add("@Zip", model.Zip);
                parameters.Add("@Gender", model.Gender);
                parameters.Add("@IsActive", model.IsActive);
                parameters.Add("@ModifiedBy", currentUserId);
                if (!string.IsNullOrWhiteSpace(model.Password))
                    parameters.Add("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(model.Password));
                else
                    parameters.Add("@PasswordHash", (object)null);
                parameters.Add("@Action", "UPDATE_MEMBER");

                var updated = await connection.QueryFirstOrDefaultAsync<UserDto>(
                    "sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Member updated successfully.";
                ResponseDto.Data = updated;
                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 503;
                ResponseDto.ResponseMsg = $"{sqlEx.Message}";
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally { connection.Close(); }
        }
        public async Task<ResponseDto> ToggleMemberActiveAsync(int id, int currentUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                // Fetch member to validate ownership and current status
                var checkParams = new DynamicParameters();
                checkParams.Add("@UserId", id);
                checkParams.Add("@Action", "SELECT");
                var member = await connection.QueryFirstOrDefaultAsync<UserDto>(
                    "sp_UserCRUD", checkParams, commandType: CommandType.StoredProcedure);

                if (member == null || member.ParentId != currentUserId)
                {
                    ResponseDto.Status = 403;
                    ResponseDto.ResponseMsg = "Access denied: You can only manage your own members.";
                    return ResponseDto;
                }

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", id);
                parameters.Add("@IsActive", !member.IsActive);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "TOGGLE_ACTIVE");

                await connection.ExecuteAsync("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = $"Member {(!member.IsActive ? "activated" : "deactivated")} successfully.";
                ResponseDto.Data = new { MemberId = id, IsActive = !member.IsActive };
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally { connection.Close(); }
        }
        public string CreateCheckoutSession(string? plan, int userId)
        {
            StripeConfiguration.ApiKey = "sk_test_51M9O4qKR3yBF1l8fZJ5Ll6Q7x0V0ec9i8rgqi8FMKtHTqSx3WYMvdCeOpuOdpImJ6OmhTiq6DDUhB83zumFikT2700QEmhSeSP";

            var priceId = plan switch
            {
                "starter" => "price_1Rt2KhKR3yBF1l8fWC1Q2J6T",
                "professional" => "price_1Rt2LNKR3yBF1l8f4ww3GlNh",
                _ => throw new Exception("Invalid plan")
            };

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
                SuccessUrl = $"https://localhost:7103/Payment/successpayment?session_id={{CHECKOUT_SESSION_ID}}&user_id={userId}",
                CancelUrl = "https://localhost:7103/Payment/Cancel",
            };

            var service = new SessionService();
            var session = service.Create(options);

            return session.Id;
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
            catch (Exception ex)
            {
                ResponseDto.Status = 400;
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
            catch (Exception ex)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
        }
        public async Task<ResponseDto> CheckEmailAsync(CheckEmailDto model)
        {
            var ResponseDto = new ResponseDto();

            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Email", model.Email);
                parameters.Add("@Action", "CHECK_EMAIL");

                // Returns: UserId, Email, IsRegistrationCompleted
                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (result == null)
                {
                    // No active member account found for this email
                    ResponseDto.Status = 404;
                    ResponseDto.ResponseMsg = "No active member account found with this email address.";
                    return ResponseDto;
                }

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
        public async Task<ResponseDto> CompleteRegistrationAsync(CompleteRegistrationDto model, string requestBaseUrl)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                // 1. Upload profile picture if provided
                var profilePicUrl = model.ProfilePic; 
                if (model.File != null)
                {
                    profilePicUrl = await _fileService.UploadFileAsync(model.File, "uploads");
                }

                // 2. Persist changes via SP
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", model.UserId);
                parameters.Add("@FirstName", model.FirstName);
                parameters.Add("@LastName", model.LastName);
                parameters.Add("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(model.Password));
                parameters.Add("@ProfilePic", profilePicUrl);
                parameters.Add("@Action", "COMPLETE_REGISTRATION");

                await connection.ExecuteScalarAsync<int>(
                    "sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);




                // 3. Load full user so we can issue tokens
                var selectParams = new DynamicParameters();
                selectParams.Add("@UserId", model.UserId);
                selectParams.Add("@Action", "SELECT");

                var user = await connection.QueryFirstOrDefaultAsync<UserDto>(
                    "sp_UserCRUD", selectParams, commandType: CommandType.StoredProcedure);


                if (user == null)
                {
                    ResponseDto.Status = 500;
                    ResponseDto.ResponseMsg = "Registration completed but user could not be loaded.";
                    return ResponseDto;
                }


                if (user.Roles?.Contains("Restaurant") == true && user.IntroVideoViewedOn == true)
                {
                    var updateParams = new DynamicParameters();
                    updateParams.Add("@UserId", user.UserId);
                    updateParams.Add("@IntroVideoViewedOn", 0);
                    updateParams.Add("@Action", "TOGGLE_INTRO_VIDEO_VIEWED");
                    await connection.ExecuteAsync("sp_UserCRUD", updateParams, commandType: CommandType.StoredProcedure);
                }

                // 4. Sign in with cookie + issue JWT (same as Login)
                var effectiveUserId = user.ParentId.HasValue ? user.ParentId.Value : user.UserId;
                var isParent = !user.ParentId.HasValue;

                await SignInWithCookieAsync(user, effectiveUserId, isParent);

                var tokenString = _tokenService.GenerateAccessToken(user);
                var refreshTokenString = _tokenService.GenerateRefreshToken();
                await _tokenService.SaveRefreshTokenAsync(user.UserId, refreshTokenString);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Registration completed. Welcome!";
                ResponseDto.Data = new
                {
                    UserId = effectiveUserId,
                    ActualUserId = user.UserId,
                    IsParent = isParent,
                    IsFirstTimeLoggedIn = user.IntroVideoViewedOn,
                    Roles = user.Roles,
                    ProfilePic = user.ProfilePic,
                    Token = tokenString,
                    RefreshToken = refreshTokenString,
                    IntroVideoViewedOn = user.IntroVideoViewedOn,
                };

                if (user.Roles?.Contains("Restaurant") == true && user.IntroVideoViewedOn == true)
                {
                    var updateParams = new DynamicParameters();
                    updateParams.Add("@UserId", user.UserId);
                    updateParams.Add("@IntroVideoViewedOn", 0);
                    updateParams.Add("@Action", "TOGGLE_INTRO_VIDEO_VIEWED");
                    await connection.ExecuteAsync("sp_UserCRUD", updateParams, commandType: CommandType.StoredProcedure);
                }

                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 503;
                ResponseDto.ResponseMsg = $"{sqlEx.Message}";
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

                if (user == null)
                {
                    ResponseDto.Status = 404;
                    ResponseDto.ResponseMsg = "User not found.";
                    return ResponseDto;
                }

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
        private async Task SignInWithCookieAsync(UserDto user, int effectiveUserId, bool isParent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, effectiveUserId.ToString()),
                new Claim(ClaimTypes.Role, user.Roles ?? ""),
                new Claim("FirstName", user.FirstName ?? ""),
                new Claim("LastName", user.LastName ?? ""),
                new Claim("ActualUserId", user.UserId.ToString()),
                new Claim("IsParent", isParent.ToString().ToLower())
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
