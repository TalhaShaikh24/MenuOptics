using System;
using Microsoft.AspNetCore.Http;

namespace MenuOptics.Models
{
    public class UpdateRestaurantDto
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
}
