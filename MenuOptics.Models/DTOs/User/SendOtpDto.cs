using System.ComponentModel.DataAnnotations;

namespace MenuOptics.Models.DTOs
{
    public class SendOtpDTO
    {
        [Required(ErrorMessage = "UserId is required.")]
        public int UserId { get; set; }
    }
}
