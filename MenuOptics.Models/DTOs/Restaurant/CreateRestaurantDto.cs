using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MenuOptics.Models
{
    public class CreateRestaurantDTO
    {
        public int RestaurantId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public string? Phone { get; set; }
        public string? ProfilePic { get; set; }
        public required string RestaurantName { get; set; }
        public string? SubscriptionType { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public IFormFile? File { get; set; }
    }
}
