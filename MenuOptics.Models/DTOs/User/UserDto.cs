using System;

namespace MenuOptics.Models
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public required string Username { get; set; }
        public string? PasswordHash { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? ProfilePic { get; set; }
        public string? Address { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? Zip { get; set; }
        public string? Gender { get; set; }
        public string? RestaurantName { get; set; }
        public string? CuisineType { get; set; }
        public string? RestaurantSize { get; set; }
        public bool IsActive { get; set; }
        public string? Roles { get; set; }
        public bool IsLinked { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? Plan { get; set; }
        public string? StripeCustomerId { get; set; }
        public string? StripeSubscriptionId { get; set; }
        public string? SubscriptionStatus { get; set; }
        public bool? IsParent { get; set; }
        public int? ParentId { get; set; }
        public bool? IntroVideoViewedOn { get; set; }
    }
}
