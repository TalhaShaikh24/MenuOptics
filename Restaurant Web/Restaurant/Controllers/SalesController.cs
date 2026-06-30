using Microsoft.AspNetCore.Mvc;

namespace Restaurant.Controllers
{
    public class SalesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
