using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Filters;
using System.Security.Claims;

namespace MenuOptics.Controllers
{
    public class RestaurantController : Controller
    {
        [AuthorizeAdmin]
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Edit()
        {
            return View();
        }

        [AuthorizeRestaurant]
        public IActionResult Members()
        {
            // Only the parent account (IsParent = true) can manage members
            var isParentClaim = User.FindFirst("IsParent")?.Value;
            if (string.Equals(isParentClaim, "false",StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("AccessDenied", "User");
            }
            return View();
        }
    }
}
