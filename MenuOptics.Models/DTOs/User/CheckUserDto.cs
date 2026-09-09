using System.ComponentModel.DataAnnotations;

namespace MenuOptics.Models
{
    public class CheckUserDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = string.Empty;
    }
}
