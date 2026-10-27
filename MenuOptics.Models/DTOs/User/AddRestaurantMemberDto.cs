using System.ComponentModel.DataAnnotations;

namespace MenuOptics.Models.DTOs
{
    public class AddRestaurantMemberDTO
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? Zip { get; set; }

        // Only used when called by Admin to specify which restaurant owner this member belongs to
        public int? ParentId { get; set; }
    }
}
