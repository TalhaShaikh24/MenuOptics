using Microsoft.AspNetCore.Mvc;

namespace Restaurant.Controllers
{
    public class Restaurant : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
