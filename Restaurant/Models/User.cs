using System.ComponentModel.DataAnnotations;

namespace MenuOptics.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? ProfilePic { get; set; }
        public string? Address { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? Zip { get; set; }
        public string? Gender { get; set; }
        public string? RestaurantName { get; set; }
        public string? CuisineType { get; set; }
        public string? RestaurantSize { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public IFormFile? File { get; set; }
        public string? Plan { get; set; }
        public bool IsLinked { get; set; }
        public string? StripeCustomerId { get; set; }
        public string? StripeSubscriptionId { get; set; }
        public string? SubscriptionStatus { get; set; }
        public bool? IsParent { get; set; }
        public int? ParentId { get; set; }
    }


    public class UserDTO
    {
        public int UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public required string Username { get; set; }
        public string? PasswordHash { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? ProfilePic { get; set; }
        public string? Address { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? Zip { get; set; }
        public string? Gender { get; set; }
        public string? RestaurantName { get; set; }
        public string? CuisineType { get; set; }
        public string? RestaurantSize { get; set; }
        public bool IsActive { get; set; }
        public string? Roles { get; set; }
        public bool IsLinked { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? Plan { get; set; }
        public string? StripeCustomerId { get; set; }
        public string? StripeSubscriptionId { get; set; }
        public string? SubscriptionStatus { get; set; }
        public bool? IsParent { get; set; }
        public int? ParentId { get; set; }
        // ── NEW ──────────────────────────────────────────────────────
        public bool? IsRegistrationCompleted { get; set; }
    }


    public class CreateRestaurant
    {
        [Required(ErrorMessage = "restaurantId is required.")]
        public int RestaurantId { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        [Required(ErrorMessage = "username is required.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "email is required.")]
        public string Email { get; set; }

        public string? Phone { get; set; }
        public string? ProfilePic { get; set; }

        [Required(ErrorMessage = "restaurantName is required.")]
        public string RestaurantName { get; set; }

        public DateTime CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public IFormFile? File { get; set; }
    }
    public class UpdateRestaurant
    {
        public int RestaurantId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? ProfilePic { get; set; }
        public string? RestaurantName { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public IFormFile? File { get; set; }
    }

    public class UserStats
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
    }

    public class LoginModel
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }



    public class Response
    {
        public int Status { get; set; }
        public string? ResponseMsg { get; set; }
        public object? Data { get; set; }
    }

    public class RefreshToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RefreshTokenRequest
    {
        public string? RefreshToken { get; set; }
    }

    public class AddRestaurantMemberDTO
    {
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? Zip { get; set; }
        public string? Gender { get; set; }

        // Only used when called by Admin to specify which restaurant owner this member belongs to
        public int? ParentId { get; set; }
    }

    public class UpdateRestaurantMemberDTO
    {
        [Required]
        public int MemberId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Address { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? Zip { get; set; }
        public string? Gender { get; set; }
        public string? Password { get; set; }
        public bool? IsActive { get; set; }
        public IFormFile? File { get; set; }
        public string? ProfilePic { get; set; }
    }

    // ── NEW: used by the CheckEmail endpoint ─────────────────────────
    public class CheckEmailModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = string.Empty;
    }

    // ── NEW: used by the CompleteRegistration endpoint ───────────────
    public class CompleteRegistrationDTO
    {
        [Required(ErrorMessage = "UserId is required.")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        // Profile picture uploaded as a file (multipart/form-data)
        public IFormFile? File { get; set; }

        // Existing URL if already set (kept for re-use / no re-upload)
        public string? ProfilePic { get; set; }
    }


    // ── OTP: Send ────────────────────────────────────────────────────
    public class SendOtpDTO
    {
        [Required(ErrorMessage = "UserId is required.")]
        public int UserId { get; set; }
    }

    // ── OTP: Verify ──────────────────────────────────────────────────
    public class VerifyOtpDTO
    {
        [Required(ErrorMessage = "UserId is required.")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "OTP is required.")]
        public string Otp { get; set; } = string.Empty;
    }


    public class RestaurantStatusRequest
    {
            public bool IsActive { get; set; } 
    }

    public class SetPasswordDTO
    {
        public long RestaurantId { get; set; }

        [Required]
        public string Password { get; set; }
    }
}