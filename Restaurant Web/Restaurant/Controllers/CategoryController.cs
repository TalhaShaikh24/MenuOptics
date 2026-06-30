using Microsoft.AspNetCore.Mvc;

namespace Restaurant.Controllers
{
    public class CategoryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
