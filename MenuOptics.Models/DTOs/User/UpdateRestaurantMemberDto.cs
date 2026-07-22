using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MenuOptics.Models
{
    public class UpdateRestaurantMemberDto
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
}
