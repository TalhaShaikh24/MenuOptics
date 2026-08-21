using Microsoft.AspNetCore.Mvc;
using MenuOptics.Filters;
using System.Security.Claims;

namespace MenuOptics.Controllers
{
    [AuthorizeRestaurant]
    public class SubscriptionController : Controller
    {
        public IActionResult Index()
        {
            // If restaurant was created by a Vendor, it is free to use with no subscription management
            var isVendorCreated = User.FindFirst("IsVendorCreated")?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            if (isVendorCreated)
            {
                return RedirectToAction("Index", "Home");
            }

            // Only the parent restaurant account (IsParent = true) can access and manage subscriptions
            var isParentClaim = User.FindFirst("IsParent")?.Value;
            if (isParentClaim != null && isParentClaim.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                // Staff members inherit parent subscription and cannot manage it directly
                return RedirectToAction("Index", "Menu");
            }

            return View();
        }
    }
}
