using System.ComponentModel.DataAnnotations;

namespace MenuOptics.Models
{
    public class SetPasswordDto
    {
        public long RestaurantId { get; set; }

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
