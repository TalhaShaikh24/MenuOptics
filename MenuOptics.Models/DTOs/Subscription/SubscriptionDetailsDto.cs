using System;

namespace MenuOptics.Models.DTOs.Subscription
{
    public class SubscriptionDetailsDto
    {
        public int UserId { get; set; }
        public string? RestaurantName { get; set; }
        public string? Email { get; set; }
        public string? Plan { get; set; }
        public string? SubscriptionStatus { get; set; }
        public string? StripeCustomerId { get; set; }
        public string? StripeSubscriptionId { get; set; }
        public DateTime? CurrentPeriodStart { get; set; }
        public DateTime? CurrentPeriodEnd { get; set; }
        public bool? CancelAtPeriodEnd { get; set; }
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public string? Interval { get; set; }
        public bool IsParent { get; set; }
        public int? ParentId { get; set; }
        public string? ParentRestaurantName { get; set; }
        public bool IsActiveSubscription { get; set; }
        public string? LatestInvoiceStatus { get; set; }
        public string? PaymentMethodLast4 { get; set; }
        public string? PaymentMethodBrand { get; set; }
    }

    public class RenewSubscriptionRequestDto
    {
        public string? Plan { get; set; }
    }
}
