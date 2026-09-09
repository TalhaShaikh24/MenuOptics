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
using MenuOptics.Models.Enums;

namespace MenuOptics.Services
{
    public class UserService : IUserService
    {
        private readonly DapperContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IAuthService _authService;
        private readonly IActivityLogService _activityLog;
        private readonly ITokenService _tokenService;
        private readonly ISmtpEmailService _emailService;
        private readonly IFileUploadService _fileService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(
            DapperContext context,
            IWebHostEnvironment environment,
            IAuthService authService,
            IActivityLogService activityLog,
            ITokenService tokenService,
            ISmtpEmailService emailService,
            IFileUploadService fileService,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _environment = environment;
            _authService = authService;
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

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "User stats retrieved successfully.";
                ResponseDto.Data = stats;
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

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Restaurant stats retrieved successfully.";
                ResponseDto.Data = stats;
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

        public async Task<ResponseDto> CreateUserAsync(User model, int fallbackCreatedBy, string requestBaseUrl)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                string? filePathUrl = null;

                if (model.File != null)
                {
                    filePathUrl = await _fileService.UploadFileAsync(model.File, "uploads");
                }
                string abc = Roles.Restaurant.ToString();
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
                parameters.Add("@VendorId", model.VendorId);
                parameters.Add("@SecondaryVendor", model.SecondaryVendor);
                parameters.Add("@VendorCustomerNumber", model.VendorCustomerNumber);
                parameters.Add("@RestaurantName", model.Role == Roles.Restaurant.ToString() ? model.RestaurantName : null);
                parameters.Add("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(model.Password));
                parameters.Add("@RoleName", model.Role);
                parameters.Add("@IsRegistrationCompleted", 1);
                parameters.Add("@SubscriptionType", model.Role == Roles.Vendor.ToString() || model.Role == Roles.Admin.ToString() ? null : "CORE");
                parameters.Add("@SubscriptionStatus", model.Role == Roles.Vendor.ToString() || model.Role == Roles.Admin.ToString() ? null : "Pending");
                parameters.Add("@IsParent", model.Role == Roles.Vendor.ToString() || model.Role == Roles.Admin.ToString() ? null : 1);
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

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "User profile retrieved successfully.";
                ResponseDto.Data = userProfile;
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

        public async Task<ResponseDto> UpdateUserAsync(User model, int modifiedBy, string requestBaseUrl)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {

                string? filePathUrl = null;

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
                parameters.Add("@RestaurantName", model.Role == Roles.Restaurant.ToString() ? model.RestaurantName : null);
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

        public async Task<ResponseDto> AddRestaurantMemberAsync(AddRestaurantMemberDto model, int currentUserId, string callerRole, string isParentClaim)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                // Restaurant owners who are members (IsParent=false) cannot create sub-members
                if (callerRole.Contains(Roles.Restaurant.ToString()) && isParentClaim != "true")
                {
                    ResponseDto.Status = 403;
                    ResponseDto.ResponseMsg = "Only the parent restaurant account can add members.";
                    return ResponseDto;
                }

                int resolvedParentId;
                if (callerRole.Contains(Roles.Admin.ToString()))
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
            finally { connection.Close(); }
        }

        public async Task<ResponseDto> UpdateRestaurantMemberAsync(UpdateRestaurantMemberDto model, int currentUserId, string requestBaseUrl)
        {
            var ResponseDto = new ResponseDto();

            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                // Verify the member belongs to this parent
                var checkParams = new DynamicParameters();
                checkParams.Add("@UserId", model.MemberId);
                checkParams.Add("@Action", "SELECT");

                //QueryFirstAsync means it will always expect a record or the exception will be thrown from Store Procedure
                var member = await connection.QueryFirstAsync<UserDto>(
                    "sp_UserCRUD", checkParams, commandType: CommandType.StoredProcedure);

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
                parameters.Add("@ModifiedBy", currentUserId);
                if (!string.IsNullOrWhiteSpace(model.Password))
                    parameters.Add("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(model.Password));
                else
                    parameters.Add("@PasswordHash", null);
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

                //QueryFirstAsync means it will always expect a record or the exception will be thrown from Store Procedure
                var member = await connection.QueryFirstAsync<UserDto>(
                    "sp_UserCRUD", checkParams, commandType: CommandType.StoredProcedure);

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", id);
                parameters.Add("@IsActive", !member.IsActive);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "TOGGLE_ACTIVE_MEMBER");

                await connection.ExecuteAsync("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = $"Member {(!member.IsActive ? "activated" : "deactivated")} successfully.";
                ResponseDto.Data = new { MemberId = id, IsActive = !member.IsActive };
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
            finally { connection.Close(); }
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


                if (user.Roles?.Contains(Roles.Restaurant.ToString()) == true && user.IntroVideoViewedOn == true)
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

                await _authService.SignInWithCookieAsync(user, effectiveUserId, isParent);

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

                if (user.Roles?.Contains(Roles.Restaurant.ToString()) == true && user.IntroVideoViewedOn == true)
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
    }
}
