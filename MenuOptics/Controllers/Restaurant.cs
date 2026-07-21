using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Filters;
using System.Security.Claims;

namespace MenuOptics.Controllers
{
    public class Restaurant : Controller
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
            if (isParentClaim != "true")
            {
                return RedirectToAction("AccessDenied", "User");
            }
            return View();
        }
    }
}
