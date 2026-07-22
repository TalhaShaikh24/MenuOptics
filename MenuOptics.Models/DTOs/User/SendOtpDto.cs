using System.ComponentModel.DataAnnotations;

namespace MenuOptics.Models
{
    public class SendOtpDto
    {
        [Required(ErrorMessage = "UserId is required.")]
        public int UserId { get; set; }
    }
}
