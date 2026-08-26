using System;
using Microsoft.AspNetCore.Http;

namespace MenuOptics.Models
{
    public class UpdateRestaurantDto
    {
        public int RestaurantId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? RestaurantName { get; set; }
        public string? SubscriptionType { get; set; }
    }
}
