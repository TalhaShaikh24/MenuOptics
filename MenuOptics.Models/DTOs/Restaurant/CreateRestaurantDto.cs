using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MenuOptics.Models
{
    public class CreateRestaurantDto
    {
        [Required(ErrorMessage = "restaurantId is required.")]
        public int RestaurantId { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        [Required(ErrorMessage = "username is required.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "email is required.")]
        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }
        public string? ProfilePic { get; set; }

        [Required(ErrorMessage = "restaurantName is required.")]
        public string RestaurantName { get; set; } = string.Empty;
        public string? SubscriptionType { get; set; }

        public DateTime CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public IFormFile? File { get; set; }
    }
}
