using MenuOptics.Models.Enums;
using System;

namespace MenuOptics.Models
{
    public class GetUserDTO
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
        public string? RestaurantName { get; set; }
        public bool IsActive { get; set; }
        public string? Roles { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? StripeCustomerId { get; set; }
        public string? StripeSubscriptionId { get; set; }
        public string? SubscriptionType { get; set; }
        public string? SubscriptionStatus { get; set; }
        public string? SecondaryVendor { get; set; }
        public string? VendorCustomerNumber { get; set; }
        public bool? IsParent { get; set; }
        public int? ParentId { get; set; }
        public string? PaidBy { get; set; }
        public bool? HasViewedIntroVideo { get; set; }
        public int? CreatedBy { get; set; }
        public int? VendorId { get; set; }
    }
}
