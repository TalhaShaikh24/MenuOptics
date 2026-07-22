using System.ComponentModel.DataAnnotations;

namespace MenuOptics.Models
{
    public class VerifyOtpDto
    {
        [Required(ErrorMessage = "UserId is required.")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "OTP is required.")]
        public string Otp { get; set; } = string.Empty;
    }
}
