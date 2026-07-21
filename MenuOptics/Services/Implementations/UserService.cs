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
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(
            DapperContext context,
            IWebHostEnvironment environment,
            IActivityLogService activityLog,
            ITokenService tokenService,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _environment = environment;
            _activityLog = activityLog;
            _tokenService = tokenService;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Response> GetUserStatsAsync()
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_USER_STATS");

                var stats = await connection.QueryFirstOrDefaultAsync<UserStats>(
                    "sp_UserCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                if (stats == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "No user stats available.";
                    return response;
                }

                response.Status = 200;
                response.ResponseMsg = "User stats retrieved successfully.";
                response.Data = stats;
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> GetRestaurantStatsAsync()
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_RESTAURANT_STATS");

                var stats = await connection.QueryFirstOrDefaultAsync<UserStats>(
                    "sp_UserCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                if (stats == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "No restaurant stats available.";
                    return response;
                }

                response.Status = 200;
                response.ResponseMsg = "Restaurant stats retrieved successfully.";
                response.Data = stats;
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> CreateUserAsync(User model, int fallbackCreatedBy, string requestBaseUrl)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var filePathUrl = "";
                if (model.File != null)
                {
                    filePathUrl = await SaveUploadedFileAsync(model.File, requestBaseUrl);
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
                parameters.Add("@RestaurantName", model.Role == "restaurant" || model.Role == "Restaurant" || model.Role == "admin" ? model.RestaurantName : null);
                parameters.Add("@CuisineType", model.Role == "restaurant" || model.Role == "Restaurant" || model.Role == "admin" ? model.CuisineType : null);
                parameters.Add("@RestaurantSize", model.Role == "restaurant" || model.Role == "Restaurant" || model.Role == "admin" ? model.RestaurantSize : null);
                parameters.Add("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(model.Password));
                parameters.Add("@RoleName", model.Role);
                parameters.Add("@IsActive", model.IsActive);
                parameters.Add("@SubscriptionStatus", model.Role == "Vendor" ? null : "Pending");
                parameters.Add("@CreatedBy", model.CreatedBy ?? fallbackCreatedBy);
                parameters.Add("@Action", "INSERT");

                int userId = await connection.ExecuteScalarAsync<int>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                response.Status = 200;
                response.ResponseMsg = "User created successfully.";
                response.Data = new { UserId = userId };
                return response;
            }
            catch (SqlException sqlEx)
            {
                response.Status = 400;
                response.ResponseMsg = $"{sqlEx.Message}";
                return response;
            }
            catch (InvalidOperationException invalidOpEx)
            {
                response.Status = 400;
                response.ResponseMsg = $"{invalidOpEx.Message}";
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 400;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> GetUserAsync(int id)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", id);
                parameters.Add("@Action", "SELECT");

                var userProfile = await connection.QueryFirstOrDefaultAsync<UserDTO>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (userProfile == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "User profile not found.";
                    return response;
                }

                response.Status = 200;
                response.ResponseMsg = "User profile retrieved successfully.";
                response.Data = userProfile;
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> GetAllUsersAsync()
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL");

                var users = await connection.QueryAsync<UserDTO>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                response.Status = 200;
                response.ResponseMsg = "Users retrieved successfully.";
                response.Data = users;
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> GetAllRestaurantsAsync()
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL_RESTAURANT");

                var users = await connection.QueryAsync<UserDTO>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                response.Status = 200;
                response.ResponseMsg = "Users retrieved successfully.";
                response.Data = users;
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> UpdateUserAsync(User model, int modifiedBy, string requestBaseUrl)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (model.UserId <= 0)
                {
                    response.Status = 400;
                    response.ResponseMsg = "UserId is required for updating a user.";
                    return response;
                }

                var filePathUrl = model.ProfilePic;
                if (model.File != null)
                {
                    filePathUrl = await SaveUploadedFileAsync(model.File, requestBaseUrl);
                }

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", model.UserId);
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
                parameters.Add("@RestaurantName", model.Role == "Restaurant" || model.Role == "Customer" ? model.RestaurantName : null);
                parameters.Add("@CuisineType", model.Role == "Restaurant" || model.Role == "Customer" ? model.CuisineType : null);
                parameters.Add("@RestaurantSize", model.Role == "Restaurant" || model.Role == "Customer" ? model.RestaurantSize : null);
                parameters.Add("@RoleName", model.Role);
                parameters.Add("@IsActive", model.IsActive);
                parameters.Add("@ModifiedBy", modifiedBy);
                parameters.Add("@Action", "UPDATE");

                int userId = await connection.ExecuteScalarAsync<int>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                response.Status = 200;
                response.ResponseMsg = "User updated successfully.";
                response.Data = new { UserId = userId };
                return response;
            }
            catch (SqlException sqlEx)
            {
                response.Status = 503;
                response.ResponseMsg = $"Expection: {sqlEx.Message}";
                return response;
            }
            catch (InvalidOperationException invalidOpEx)
            {
                response.Status = 422;
                response.ResponseMsg = $"Invalid operation: {invalidOpEx.Message}";
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> ToggleUserActiveAsync(int id, int currentUserId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", id);
                parameters.Add("@Action", "SELECT");
                var user = await connection.QueryFirstOrDefaultAsync<UserDTO>("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (user == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "User not found.";
                    return response;
                }

                parameters = new DynamicParameters();
                parameters.Add("@UserId", id);
                parameters.Add("@IsActive", !user.IsActive);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "TOGGLE_ACTIVE");

                await connection.ExecuteAsync("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                response.Status = 200;
                response.ResponseMsg = $"User {(user.IsActive ? "deactivated" : "activated")} successfully.";
                response.Data = new { UserId = id, IsActive = !user.IsActive };
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 400;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> DeleteUserAsync(int id)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", id);
                parameters.Add("@Action", "DELETE");

                var affectedRows = await connection.ExecuteAsync("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);
                if (affectedRows == 0)
                {
                    response.Status = 404;
                    response.ResponseMsg = "User not found.";
                    return response;
                }

                response.Status = 200;
                response.ResponseMsg = "User deleted successfully.";
                response.Data = new { UserId = id };
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> LoginAsync(LoginModel model, string requestId)
        {
            var response = new Response();
            var payload = System.Text.Json.JsonSerializer.Serialize(new { model.Username }); // never log password

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Username", model.Username);
                parameters.Add("@Action", "LOGIN");

                var user = await connection.QueryFirstOrDefaultAsync<UserDTO>(
                    "sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (user == null || user.Roles == null)
                {
                    response.Status = 401;
                    response.ResponseMsg = "Invalid username.";
                    return response;
                }

                if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    response.Status = 401;
                    response.ResponseMsg = "Invalid password.";
                    return response;
                }

                var effectiveUserId = user.ParentId.HasValue ? user.ParentId.Value : user.UserId;
                var isParent = !user.ParentId.HasValue;

                await SignInWithCookieAsync(user, effectiveUserId, isParent);

                var tokenString = _tokenService.GenerateAccessToken(user);
                var refreshTokenString = _tokenService.GenerateRefreshToken();
                await _tokenService.SaveRefreshTokenAsync(user.UserId, refreshTokenString);

                response.Status = 200;
                response.ResponseMsg = "Login successful.";
                response.Data = new
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
                    RefreshToken = refreshTokenString
                };

                if (user.Roles?.Contains("Vendor") == true)
                {
                    await _activityLog.LogAsync(user.UserId, requestId, "POST /api/UserApi/Login", payload,
                        System.Text.Json.JsonSerializer.Serialize(new { response.Status, response.ResponseMsg }), "Success");
                }

                return response;
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower() == "invalidusername")
                {
                    response.Status = 401;
                    response.ResponseMsg = "Invalid Username";
                    return response;
                }

                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> LogoutAsync()
        {
            var response = new Response();
            await _httpContextAccessor.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            response.Status = 200;
            response.ResponseMsg = "Logout successful.";
            response.Data = null;
            return response;
        }

        public async Task<Response> AddRestaurantMemberAsync(AddRestaurantMemberDTO model, int currentUserId, string callerRole, string isParentClaim)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                // Restaurant owners who are members (IsParent=false) cannot create sub-members
                if (callerRole.Contains("Restaurant") && isParentClaim != "true")
                {
                    response.Status = 403;
                    response.ResponseMsg = "Only the parent restaurant account can add members.";
                    return response;
                }

                int resolvedParentId;
                if (callerRole.Contains("Admin"))
                {
                    if (!model.ParentId.HasValue || model.ParentId <= 0)
                    {
                        response.Status = 400;
                        response.ResponseMsg = "Admin must provide a valid ParentId (the restaurant owner's UserId).";
                        return response;
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

                response.Status = 200;
                response.ResponseMsg = "Restaurant member added successfully.";
                response.Data = new { MemberId = newMemberId };
                return response;
            }
            catch (SqlException sqlEx)
            {
                response.Status = 503;
                response.ResponseMsg = $"{sqlEx.Message}";
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
            finally { connection.Close(); }
        }

        public async Task<Response> GetRestaurantMembersAsync(int currentUserId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@ParentId", currentUserId);
                parameters.Add("@Action", "SELECT_MEMBERS");

                var members = await connection.QueryAsync<UserDTO>(
                    "sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Members retrieved successfully.";
                response.Data = members;
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
            finally { connection.Close(); }
        }

        public async Task<Response> GetMemberStatsAsync(int currentUserId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@ParentId", currentUserId);
                parameters.Add("@Action", "SELECT_MEMBER_STATS");

                var stats = await connection.QueryFirstOrDefaultAsync<UserStats>(
                    "sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Member stats retrieved successfully.";
                response.Data = stats;
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
            finally { connection.Close(); }
        }

        public async Task<Response> UpdateRestaurantMemberAsync(UpdateRestaurantMemberDTO model, int currentUserId, string requestBaseUrl)
        {
            var response = new Response();

            if (model.MemberId <= 0)
            {
                response.Status = 400;
                response.ResponseMsg = "MemberId is required.";
                return response;
            }

            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                // Verify the member belongs to this parent
                var checkParams = new DynamicParameters();
                checkParams.Add("@UserId", model.MemberId);
                checkParams.Add("@Action", "SELECT");
                var member = await connection.QueryFirstOrDefaultAsync<UserDTO>(
                    "sp_UserCRUD", checkParams, commandType: CommandType.StoredProcedure);

                if (member == null || member.ParentId != currentUserId)
                {
                    response.Status = 403;
                    response.ResponseMsg = "Access denied: You can only update your own members.";
                    return response;
                }

                var filePathUrl = model.ProfilePic;
                if (model.File != null)
                {
                    filePathUrl = await SaveUploadedFileAsync(model.File, requestBaseUrl);
                }

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", model.MemberId);
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
                parameters.Add("@ProfilePic", filePathUrl);
                parameters.Add("@IsActive", model.IsActive);
                parameters.Add("@ModifiedBy", currentUserId);
                if (!string.IsNullOrWhiteSpace(model.Password))
                    parameters.Add("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(model.Password));
                else
                    parameters.Add("@PasswordHash", (object)null);
                parameters.Add("@Action", "UPDATE_MEMBER");

                var updated = await connection.QueryFirstOrDefaultAsync<UserDTO>(
                    "sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Member updated successfully.";
                response.Data = updated;
                return response;
            }
            catch (SqlException sqlEx)
            {
                response.Status = 503;
                response.ResponseMsg = $"{sqlEx.Message}";
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
            finally { connection.Close(); }
        }

        public async Task<Response> ToggleMemberActiveAsync(int id, int currentUserId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                // Fetch member to validate ownership and current status
                var checkParams = new DynamicParameters();
                checkParams.Add("@UserId", id);
                checkParams.Add("@Action", "SELECT");
                var member = await connection.QueryFirstOrDefaultAsync<UserDTO>(
                    "sp_UserCRUD", checkParams, commandType: CommandType.StoredProcedure);

                if (member == null || member.ParentId != currentUserId)
                {
                    response.Status = 403;
                    response.ResponseMsg = "Access denied: You can only manage your own members.";
                    return response;
                }

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", id);
                parameters.Add("@IsActive", !member.IsActive);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "TOGGLE_ACTIVE");

                await connection.ExecuteAsync("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = $"Member {(!member.IsActive ? "activated" : "deactivated")} successfully.";
                response.Data = new { MemberId = id, IsActive = !member.IsActive };
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
            finally { connection.Close(); }
        }

        public async Task<Response> DeleteMemberAsync(int id, int currentUserId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                // Validate ownership
                var checkParams = new DynamicParameters();
                checkParams.Add("@UserId", id);
                checkParams.Add("@Action", "SELECT");
                var member = await connection.QueryFirstOrDefaultAsync<UserDTO>(
                    "sp_UserCRUD", checkParams, commandType: CommandType.StoredProcedure);

                if (member == null || member.ParentId != currentUserId)
                {
                    response.Status = 403;
                    response.ResponseMsg = "Access denied: You can only delete your own members.";
                    return response;
                }

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", id);
                parameters.Add("@Action", "DELETE");
                await connection.ExecuteAsync("sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Member deleted successfully.";
                response.Data = new { MemberId = id };
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return response;
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

        public async Task<Response> RefreshTokenAsync(RefreshTokenRequest model)
        {
            var response = new Response();

            if (string.IsNullOrWhiteSpace(model?.RefreshToken))
            {
                response.Status = 400;
                response.ResponseMsg = "Refresh token is required.";
                return response;
            }

            try
            {
                // Validate the refresh token from DB
                var storedToken = await _tokenService.GetRefreshTokenAsync(model.RefreshToken);

                if (storedToken == null)
                {
                    response.Status = 401;
                    response.ResponseMsg = "Invalid refresh token.";
                    return response;
                }

                if (storedToken.IsRevoked)
                {
                    response.Status = 401;
                    response.ResponseMsg = "Refresh token has been revoked.";
                    return response;
                }

                if (storedToken.ExpiresAt < DateTime.UtcNow)
                {
                    response.Status = 401;
                    response.ResponseMsg = "Refresh token has expired. Please log in again.";
                    return response;
                }

                // Fetch user data to build a fresh access token
                using var connection = _context.CreateConnection();
                connection.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", storedToken.UserId);
                parameters.Add("@Action", "SELECT");

                var user = await connection.QueryFirstOrDefaultAsync<UserDTO>(
                    "sp_UserCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (user == null || !user.IsActive)
                {
                    response.Status = 401;
                    response.ResponseMsg = "User not found or account is inactive.";
                    return response;
                }

                // Rotate tokens: revoke old, issue new pair
                await _tokenService.RevokeRefreshTokenAsync(model.RefreshToken);

                var newAccessToken = _tokenService.GenerateAccessToken(user);
                var newRefreshToken = _tokenService.GenerateRefreshToken();
                await _tokenService.SaveRefreshTokenAsync(user.UserId, newRefreshToken);

                response.Status = 200;
                response.ResponseMsg = "Token refreshed successfully.";
                response.Data = new
                {
                    Token = newAccessToken,
                    RefreshToken = newRefreshToken
                };

                return response;
            }
            catch (Exception ex)
            {
                response.Status = 400;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
        }

        public async Task<Response> RevokeTokenAsync(RefreshTokenRequest model)
        {
            var response = new Response();

            if (string.IsNullOrWhiteSpace(model?.RefreshToken))
            {
                response.Status = 400;
                response.ResponseMsg = "Refresh token is required.";
                return response;
            }

            try
            {
                var storedToken = await _tokenService.GetRefreshTokenAsync(model.RefreshToken);

                if (storedToken == null || storedToken.IsRevoked)
                {
                    response.Status = 400;
                    response.ResponseMsg = "Token is already invalid or does not exist.";
                    return response;
                }

                await _tokenService.RevokeRefreshTokenAsync(model.RefreshToken);

                response.Status = 200;
                response.ResponseMsg = "Token revoked successfully.";
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 400;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
        }

        public async Task<Response> CheckEmailAsync(CheckEmailModel model)
        {
            var response = new Response();

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
                    response.Status = 404;
                    response.ResponseMsg = "No active member account found with this email address.";
                    return response;
                }

                // Nullable: treat NULL as not completed (first-time member)
                bool isCompleted = result.IsRegistrationCompleted == true;

                response.Status = 200;
                response.ResponseMsg = "Account found.";
                response.Data = new
                {
                    UserId = (int)result.UserId,
                    Username = (string)result.Username,
                    IsRegistrationCompleted = isCompleted
                };
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> CompleteRegistrationAsync(CompleteRegistrationDTO model, string requestBaseUrl)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                // 1. Upload profile picture if provided
                var profilePicUrl = model.ProfilePic; // keep existing if no new file
                if (model.File != null)
                {
                    profilePicUrl = await SaveUploadedFileAsync(model.File, requestBaseUrl);
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

                var user = await connection.QueryFirstOrDefaultAsync<UserDTO>(
                    "sp_UserCRUD", selectParams, commandType: CommandType.StoredProcedure);

                if (user == null)
                {
                    response.Status = 500;
                    response.ResponseMsg = "Registration completed but user could not be loaded.";
                    return response;
                }

                // 4. Sign in with cookie + issue JWT (same as Login)
                var effectiveUserId = user.ParentId.HasValue ? user.ParentId.Value : user.UserId;
                var isParent = !user.ParentId.HasValue;

                await SignInWithCookieAsync(user, effectiveUserId, isParent);

                var tokenString = _tokenService.GenerateAccessToken(user);
                var refreshTokenString = _tokenService.GenerateRefreshToken();
                await _tokenService.SaveRefreshTokenAsync(user.UserId, refreshTokenString);

                response.Status = 200;
                response.ResponseMsg = "Registration completed. Welcome!";
                response.Data = new
                {
                    UserId = effectiveUserId,
                    ActualUserId = user.UserId,
                    IsParent = isParent,
                    Roles = user.Roles,
                    ProfilePic = user.ProfilePic,
                    Token = tokenString,
                    RefreshToken = refreshTokenString
                };
                return response;
            }
            catch (SqlException sqlEx)
            {
                response.Status = 503;
                response.ResponseMsg = $"{sqlEx.Message}";
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> SendOtpAsync(SendOtpDTO model)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                // 1. Load the user's email address
                var selectParams = new DynamicParameters();
                selectParams.Add("@UserId", model.UserId);
                selectParams.Add("@Action", "SELECT");

                var user = await connection.QueryFirstOrDefaultAsync<UserDTO>(
                    "sp_UserCRUD", selectParams, commandType: CommandType.StoredProcedure);

                if (user == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "User not found.";
                    return response;
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

                response.Status = 200;
                response.ResponseMsg = "OTP sent to your email.";
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<Response> VerifyOtpAsync(VerifyOtpDTO model)
        {
            var response = new Response();
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
                    response.Status = 200;
                    response.ResponseMsg = "OTP verified successfully.";
                    response.Data = new { IsValid = true };
                    return response;
                }

                response.Status = 400;
                response.ResponseMsg = "Invalid or expired OTP.";
                response.Data = new { IsValid = false };
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return response;
            }
            finally
            {
                connection.Close();
            }
        }

        /// <summary>
        /// Saves an uploaded file under wwwroot/Uploads and returns its absolute URL.
        /// </summary>
        private async Task<string> SaveUploadedFileAsync(IFormFile file, string requestBaseUrl)
        {
            string uploadsFolder = Path.Combine(_environment.WebRootPath ?? Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return $"{requestBaseUrl}/Uploads/{fileName}";
        }

        /// <summary>
        /// Signs the user in with the cookie authentication scheme (same claims as the original Login flow).
        /// </summary>
        private async Task SignInWithCookieAsync(UserDTO user, int effectiveUserId, bool isParent)
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
