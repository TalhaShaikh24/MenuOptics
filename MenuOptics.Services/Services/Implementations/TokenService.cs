using Dapper;
using Microsoft.IdentityModel.Tokens;
using MenuOptics.Data;
using MenuOptics.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MenuOptics.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly DapperContext _context;

        public TokenService(IConfiguration configuration, DapperContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public string GenerateAccessToken(UserDto user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expiryHours = double.Parse(jwtSettings["ExpiryInHours"] ?? "1");

            var securityKey = new SymmetricSecurityKey(key);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // If the user is a member (has ParentId), use the parent's UserId as the effective identity
            // so all existing business logic that reads NameIdentifier continues to work unchanged.
            var effectiveUserId = user.ParentId.HasValue ? user.ParentId.Value : user.UserId;
            var isParent = !user.ParentId.HasValue;

            var isVendorCreated = user.VendorId.HasValue && user.VendorId.Value > 0;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username!),
                new Claim(ClaimTypes.NameIdentifier, effectiveUserId.ToString()),
                new Claim(ClaimTypes.Role, user.Roles ?? "User"),
                new Claim("FirstName", user.FirstName ?? ""),
                new Claim("LastName", user.LastName ?? ""),
                new Claim("ActualUserId", user.UserId.ToString()),
                new Claim("IsParent", isParent.ToString().ToLower()),
                new Claim("IsVendorCreated", isVendorCreated.ToString().ToLower()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expiryHours),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }


        public async Task SaveRefreshTokenAsync(int userId, string refreshToken)
        {
            var expiryDays = int.Parse(
                _configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");

            using var connection = _context.CreateConnection();

            // Revoke all existing active refresh tokens for this user
            await connection.ExecuteAsync(
                @"UPDATE UserRefreshTokens 
                  SET IsRevoked = 1 
                  WHERE UserId = @UserId AND IsRevoked = 0",
                new { UserId = userId });

            // Insert the new refresh token
            await connection.ExecuteAsync(
                @"INSERT INTO UserRefreshTokens (UserId, Token, ExpiresAt, IsRevoked, CreatedAt)
                  VALUES (@UserId, @Token, @ExpiresAt, 0, @CreatedAt)",
                new
                {
                    UserId = userId,
                    Token = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
                    CreatedAt = DateTime.UtcNow
                });
        }

        /// <summary>
        /// Retrieves a refresh token record from the database (returns null if not found).
        /// </summary>
        public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            using var connection = _context.CreateConnection();

            return await connection.QueryFirstOrDefaultAsync<RefreshToken>(
                @"SELECT Id, UserId, Token, ExpiresAt, IsRevoked, CreatedAt
                  FROM UserRefreshTokens
                  WHERE Token = @Token",
                new { Token = token });
        }

        /// <summary>
        /// Marks a refresh token as revoked so it cannot be reused.
        /// </summary>
        public async Task RevokeRefreshTokenAsync(string token)
        {
            using var connection = _context.CreateConnection();

            await connection.ExecuteAsync(
                @"UPDATE UserRefreshTokens 
                  SET IsRevoked = 1 
                  WHERE Token = @Token",
                new { Token = token });
        }
    }
}