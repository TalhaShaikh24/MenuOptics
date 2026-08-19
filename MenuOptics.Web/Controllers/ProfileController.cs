using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Filters;
using MenuOptics.Models;

namespace MenuOptics.Controllers
{
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {

            
            bool isParent = Convert.ToBoolean(User?.FindFirst("isParent")?.Value ?? "false");

            if (!isParent)
            {
                return RedirectToAction("Index", "Menu");
            }
            return View();
        }

        public IActionResult Faq()
        {
            bool isParent = Convert.ToBoolean(User?.FindFirst("isParent")?.Value ?? "false");

            if (!isParent)
            {
                return RedirectToAction("Index", "Menu");
            }
            return View();
        }
        
    }
}