using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuOptics.Models.DTOs
{
    public class CreateUserDTO
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public IFormFile? File { get; set; }
        public string? Address { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? Zip { get; set; }
        public string? RestaurantName { get; set; }
        public string? Password { get; set; }
        public bool IsActive { get; set; }
        public string? Role { get; set; }
        public int? CreatedBy { get; set; }
        public string? StripeCustomerId { get; set; }
        public string? StripeSubscriptionId { get; set; }
        public string? SubscriptionType { get; set; }
        public string? SubscriptionStatus { get; set; }
        public int? VendorId { get; set; }
        public string? SecondaryVendor { get; set; }
        public string? VendorCustomerNumber { get; set; }
        public bool? IsParent { get; set; }
    }
}
