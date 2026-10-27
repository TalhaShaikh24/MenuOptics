using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MenuOptics.Models.DTOs
{
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
}
