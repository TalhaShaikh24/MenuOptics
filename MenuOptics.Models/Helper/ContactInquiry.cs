using System.ComponentModel.DataAnnotations;

namespace MenuOptics.Models.Helper
{
    public class ContactInquiry
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Reason for inquiry is required.")]
        [StringLength(100)]
        public string Reason { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required.")]
        [StringLength(1000, ErrorMessage = "Message cannot exceed 1,000 characters.")]
        public string Message { get; set; } = string.Empty;

        [StringLength(50)]
        public string? PreferredContactMethod { get; set; } = "Email";
    }
}
